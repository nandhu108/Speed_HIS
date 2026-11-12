import { Eye, EyeOff, GripVertical, Pencil, Search } from 'lucide-react'
import { type ReactNode, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { IconPicker } from '../../../components/ui/IconPicker'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useMenuTree, useResetRoleMenuPosition, useRoleMenuTree, useSaveMenu, useSetRoleMenuPosition } from '../api'
import type { MenuNodeAdmin } from '../types'

function nodeMatches(node: MenuNodeAdmin, query: string): boolean {
  if (node.title.toLowerCase().includes(query)) return true
  return node.children.some((child) => nodeMatches(child, query))
}

function filterBySearch(nodes: MenuNodeAdmin[], query: string): MenuNodeAdmin[] {
  if (!query) return nodes
  return nodes
    .filter((node) => nodeMatches(node, query))
    .map((node) => (node.children.length ? { ...node, children: filterBySearch(node.children, query) } : node))
}

/** Keeps a node if it's itself granted, or has at least one granted descendant — prunes everything else. */
function filterByGranted(nodes: MenuNodeAdmin[], isGranted: (code: string) => boolean): MenuNodeAdmin[] {
  const result: MenuNodeAdmin[] = []
  for (const node of nodes) {
    const children = filterByGranted(node.children, isGranted)
    const selfGranted = node.permissionCode ? isGranted(node.permissionCode) : false
    if (selfGranted || children.length > 0) {
      result.push(children.length === node.children.length ? node : { ...node, children })
    }
  }
  return result
}

function findNodeById(nodes: MenuNodeAdmin[], id: number): MenuNodeAdmin | null {
  for (const node of nodes) {
    if (node.id === id) return node
    const found = findNodeById(node.children, id)
    if (found) return found
  }
  return null
}

/** True if `id` is `node` itself or anywhere in its subtree — blocks dropping a section into its own descendant. */
function containsNode(node: MenuNodeAdmin, id: number): boolean {
  if (node.id === id) return true
  return node.children.some((child) => containsNode(child, id))
}

/** The sibling array a node with the given parentId lives in, within the real (unfiltered) tree. */
function findSiblings(tree: MenuNodeAdmin[], parentId: number | null): MenuNodeAdmin[] {
  if (parentId === null) return tree
  for (const node of tree) {
    if (node.id === parentId) return node.children
    const nested = findSiblings(node.children, parentId)
    if (nested.length > 0) return nested
  }
  return []
}

type Scope = 'granted' | 'all'

interface NewMenuState {
  title: string
  icon: string | null
}

/**
 * Renders a menu tree with a caller-supplied control next to every node that carries a
 * permission code. When `roleId` is given (the role Permissions modal's "By menu" tab), this
 * shows that role's EFFECTIVE tree (global position, or this role's own override when one
 * exists) and supports drag-to-reposition / "New menu" — all writes there only ever affect
 * THIS role's sidebar via a MenuRoleLayout override, the shared Menu catalog (and every other
 * role's layout) stays untouched. A folder with no override anywhere just falls back to
 * wherever it lives globally, which is also why a freshly-created folder with no children yet
 * is invisible everywhere until some role actually populates it.
 *
 * Without `roleId` (the per-user permission-override view on UsersPage, which has no single
 * owning role to scope writes to) this just renders the plain global tree, read-only.
 */
export function MenuPermissionTree({
  roleId, renderControl, isGranted,
}: {
  roleId?: number
  renderControl: (permissionCode: string) => ReactNode
  isGranted: (permissionCode: string) => boolean
}) {
  const roleScoped = roleId !== undefined
  const { data: roleTree = [], isLoading: roleLoading } = useRoleMenuTree(roleId ?? 0, roleScoped)
  const { data: globalTree = [], isLoading: globalLoading } = useMenuTree(!roleScoped)
  const tree = roleScoped ? roleTree : globalTree
  const isLoading = roleScoped ? roleLoading : globalLoading
  const setPosition = useSetRoleMenuPosition(roleId ?? 0)
  const resetPosition = useResetRoleMenuPosition(roleId ?? 0)
  const createMenu = useSaveMenu()

  const [scope, setScope] = useState<Scope>('granted')
  const [search, setSearch] = useState('')
  const [draggedId, setDraggedId] = useState<number | null>(null)
  const [dragOverId, setDragOverId] = useState<number | null>(null)
  const [reordering, setReordering] = useState(false)
  const [creating, setCreating] = useState<NewMenuState | null>(null)
  const [createError, setCreateError] = useState<string | null>(null)
  const [renaming, setRenaming] = useState<MenuNodeAdmin | null>(null)
  const [renameValue, setRenameValue] = useState('')

  const dragEnabled = roleScoped && search.trim() === ''

  const submitRename = async () => {
    if (!renaming) return
    const value = renameValue.trim()
    try {
      await setPosition.mutateAsync({
        menuId: renaming.id,
        parentId: renaming.parentId,
        sortOrder: renaming.sortOrder,
        // Blank clears back to this role's default (the item's own global title).
        title: value === '' ? null : value,
        hidden: renaming.isHidden,
      })
      setRenaming(null)
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  const toggleHidden = async (node: MenuNodeAdmin) => {
    try {
      await setPosition.mutateAsync({
        menuId: node.id,
        parentId: node.parentId,
        sortOrder: node.sortOrder,
        title: node.isOverridden ? node.title : null,
        hidden: !node.isHidden,
      })
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  const handleDrop = async (target: MenuNodeAdmin) => {
    const draggedNode = draggedId !== null ? findNodeById(tree, draggedId) : null
    setDraggedId(null)
    setDragOverId(null)
    if (!draggedNode || draggedNode.id === target.id) return

    if (draggedNode.parentId === target.parentId) {
      const siblings = [...findSiblings(tree, draggedNode.parentId)]
      const fromIndex = siblings.findIndex((s) => s.id === draggedNode.id)
      const toIndex = siblings.findIndex((s) => s.id === target.id)
      if (fromIndex === -1 || toIndex === -1) return

      siblings.splice(toIndex, 0, siblings.splice(fromIndex, 1)[0])

      setReordering(true)
      try {
        await Promise.all(
          siblings.map((sibling, i) =>
            sibling.sortOrder === (i + 1) * 10
              ? Promise.resolve()
              : setPosition.mutateAsync({
                  menuId: sibling.id,
                  parentId: sibling.parentId,
                  sortOrder: (i + 1) * 10,
                  // A plain reorder must not clobber an existing rename/hide — only carry these
                  // forward if this node already has SOME override; otherwise leave them at
                  // default so we don't invent one just from a drag.
                  title: sibling.isOverridden ? sibling.title : null,
                  hidden: sibling.isHidden,
                }),
          ),
        )
      } catch (err) {
        window.alert(errorMessage(err))
      } finally {
        setReordering(false)
      }
      return
    }

    if (containsNode(draggedNode, target.id)) {
      window.alert("Can't move a section into one of its own children.")
      return
    }

    const newSiblings = target.children
    const nextSortOrder = newSiblings.length > 0 ? Math.max(...newSiblings.map((s) => s.sortOrder)) + 10 : 10

    setReordering(true)
    try {
      await setPosition.mutateAsync({
        menuId: draggedNode.id,
        parentId: target.id,
        sortOrder: nextSortOrder,
        title: draggedNode.isOverridden ? draggedNode.title : null,
        hidden: draggedNode.isHidden,
      })
    } catch (err) {
      window.alert(errorMessage(err))
    } finally {
      setReordering(false)
    }
  }

  const submitNewMenu = async () => {
    if (!creating) return
    if (!creating.title.trim()) {
      setCreateError('Name is required.')
      return
    }
    try {
      // Top-level, no page/permission — just a named folder. Drag it into position
      // (and drag pages under it) using the tree below; edit its Path/Permission later
      // from /admin/menus if it should ever become a real page link instead of a folder.
      await createMenu.mutateAsync({
        parentId: null,
        title: creating.title,
        icon: creating.icon,
        path: null,
        permissionCode: null,
        sortOrder: 0,
        isActive: true,
        hiddenByDefault: false,
      })
      setCreating(null)
      setCreateError(null)
    } catch (err) {
      setCreateError(errorMessage(err))
    }
  }

  let visibleTree = tree.filter((n) => n.isActive)
  if (scope === 'granted') visibleTree = filterByGranted(visibleTree, isGranted)
  visibleTree = filterBySearch(visibleTree, search.trim().toLowerCase())

  return (
    <div>
      <div className="mb-2 flex items-center gap-2">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-2.5 size-3.5 -translate-y-1/2 text-ink-faint" aria-hidden />
          <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Filter menus…" inputClassName="py-1.5 pl-8 text-sm" />
        </div>
        <div className="flex shrink-0 gap-1 rounded-shell-sm border border-border-soft bg-panel-2 p-0.5">
          <button
            type="button"
            onClick={() => setScope('granted')}
            className={`rounded-[4px] px-2.5 py-1.5 text-xs font-medium whitespace-nowrap ${scope === 'granted' ? 'bg-panel-solid text-ink shadow-shell-sm' : 'text-ink-faint'}`}
          >
            Granted only
          </button>
          <button
            type="button"
            onClick={() => setScope('all')}
            className={`rounded-[4px] px-2.5 py-1.5 text-xs font-medium whitespace-nowrap ${scope === 'all' ? 'bg-panel-solid text-ink shadow-shell-sm' : 'text-ink-faint'}`}
          >
            Show all
          </button>
        </div>
        {roleScoped && (
          <Button
            variant="secondary"
            onClick={() => {
              setCreateError(null)
              setCreating({ title: '', icon: null })
            }}
          >
            New menu
          </Button>
        )}
      </div>
      <p className="mb-2 text-[11px] text-ink-faint">
        {roleScoped
          ? 'Positions here (drag to reorder or move into another section) apply only to this role — every other role’s sidebar is unaffected.'
          : 'Toggling a menu item grants/denies the permission that gates it in the sidebar — items with no permission are visible to everyone and can’t be toggled here.'}
        {scope === 'granted' && ' Showing only menus this already has access to — switch to "Show all" to grant something new.'}
      </p>

      {isLoading && <Spinner />}
      {!isLoading && visibleTree.length === 0 && (
        <p className="py-6 text-center text-sm text-ink-faint">
          {scope === 'granted' ? 'No menus granted yet — switch to "Show all" to add some.' : `No menus match "${search}".`}
        </p>
      )}

      <div className="relative">
        {reordering && (
          <div className="absolute inset-0 z-10 flex items-center justify-center rounded-shell-sm bg-panel/70 backdrop-blur-sm">
            <Spinner label="Saving new position…" />
          </div>
        )}
        <div className="space-y-0.5">
          {visibleTree.map((node) => (
            <MenuPermissionNode
              key={node.id}
              node={node}
              depth={0}
              renderControl={renderControl}
              dragEnabled={dragEnabled}
              draggedId={draggedId}
              dragOverId={dragOverId}
              onDragStart={setDraggedId}
              onDragOverNode={setDragOverId}
              onDrop={handleDrop}
              onDragEnd={() => {
                setDraggedId(null)
                setDragOverId(null)
              }}
              onReset={(id) => resetPosition.mutate(id)}
              onRename={
                roleScoped
                  ? (n) => {
                      setRenameValue(n.title)
                      setRenaming(n)
                    }
                  : undefined
              }
              onToggleHidden={roleScoped ? toggleHidden : undefined}
            />
          ))}
        </div>
      </div>

      {renaming && (
        <Modal
          open
          title={`Rename for this role`}
          onClose={() => setRenaming(null)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setRenaming(null)}>
                Cancel
              </Button>
              <Button onClick={submitRename} loading={setPosition.isPending}>
                Save
              </Button>
            </>
          }
        >
          <div className="space-y-3">
            <p className="text-sm text-ink-muted">
              This only relabels the item in this role's sidebar — the shared menu catalog and every other role keep
              their own label. Leave it blank to go back to the default label.
            </p>
            <Input label="Label" value={renameValue} onChange={(e) => setRenameValue(e.target.value)} />
          </div>
        </Modal>
      )}

      {creating && (
        <Modal
          open
          title="New menu"
          onClose={() => setCreating(null)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setCreating(null)}>
                Cancel
              </Button>
              <Button onClick={submitNewMenu} loading={createMenu.isPending}>
                Create
              </Button>
            </>
          }
        >
          <div className="space-y-3">
            {createError && <p className="rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{createError}</p>}
            <p className="text-sm text-ink-muted">
              Just a name — drag it into place, and drag other menus onto it, in the list below.
            </p>
            <Input label="Name" value={creating.title} onChange={(e) => setCreating({ ...creating, title: e.target.value })} />
            <IconPicker value={creating.icon} onChange={(icon) => setCreating({ ...creating, icon })} />
          </div>
        </Modal>
      )}
    </div>
  )
}

function MenuPermissionNode({
  node, depth, renderControl, dragEnabled, draggedId, dragOverId, onDragStart, onDragOverNode, onDrop, onDragEnd, onReset, onRename, onToggleHidden,
}: {
  node: MenuNodeAdmin
  depth: number
  renderControl: (permissionCode: string) => ReactNode
  dragEnabled: boolean
  draggedId: number | null
  dragOverId: number | null
  onDragStart: (id: number) => void
  onDragOverNode: (id: number) => void
  onDrop: (node: MenuNodeAdmin) => void
  onDragEnd: () => void
  onReset: (id: number) => void
  onRename?: (node: MenuNodeAdmin) => void
  onToggleHidden?: (node: MenuNodeAdmin) => void
}) {
  return (
    <div>
      <div
        draggable={dragEnabled}
        onDragStart={(e) => {
          e.dataTransfer.effectAllowed = 'move'
          onDragStart(node.id)
        }}
        onDragOver={(e) => {
          if (!dragEnabled || draggedId === null) return
          e.preventDefault()
          onDragOverNode(node.id)
        }}
        onDrop={(e) => {
          e.preventDefault()
          onDrop(node)
        }}
        onDragEnd={onDragEnd}
        className={`flex items-center gap-2 rounded-shell-sm px-2 py-1.5 ${
          draggedId === node.id ? 'opacity-40' : ''
        } ${dragOverId === node.id && draggedId !== node.id ? 'ring-2 ring-accent-2/60' : ''}`}
        style={{ marginLeft: depth * 18 }}
      >
        {dragEnabled && <GripVertical className="size-3.5 shrink-0 cursor-grab text-ink-faint active:cursor-grabbing" aria-hidden />}
        <span
          className={`min-w-0 flex-1 text-sm ${depth === 0 ? 'font-semibold text-ink' : 'text-ink-muted'} ${node.isHidden ? 'italic opacity-50' : ''}`}
        >
          {node.title}
          {node.isHidden && ' (hidden)'}
        </span>
        {onToggleHidden && (
          <button
            type="button"
            onClick={() => onToggleHidden(node)}
            title={node.isHidden ? 'Show in this role’s sidebar' : 'Hide from this role’s sidebar'}
            className="shrink-0 text-ink-faint hover:text-ink"
          >
            {node.isHidden ? <EyeOff className="size-3.5" aria-hidden /> : <Eye className="size-3.5" aria-hidden />}
          </button>
        )}
        {onRename && (
          <button
            type="button"
            onClick={() => onRename(node)}
            title="Rename for this role"
            className="shrink-0 text-ink-faint hover:text-ink"
          >
            <Pencil className="size-3.5" aria-hidden />
          </button>
        )}
        {node.isOverridden && (
          <button
            type="button"
            onClick={() => onReset(node.id)}
            title="Reset to default position"
            className="shrink-0"
          >
            <StatusBadge tone="warning">Custom</StatusBadge>
          </button>
        )}
        {node.permissionCode ? (
          renderControl(node.permissionCode)
        ) : (
          <span className="text-[10px] text-ink-faint" title="No permission is required for this item — it shows for every role once it has a visible page inside it.">
            {node.path ? 'always visible' : 'folder'}
          </span>
        )}
      </div>
      {node.children
        .filter((c) => c.isActive)
        .map((child) => (
          <MenuPermissionNode
            key={child.id}
            node={child}
            depth={depth + 1}
            renderControl={renderControl}
            dragEnabled={dragEnabled}
            draggedId={draggedId}
            dragOverId={dragOverId}
            onDragStart={onDragStart}
            onDragOverNode={onDragOverNode}
            onDrop={onDrop}
            onDragEnd={onDragEnd}
            onReset={onReset}
            onRename={onRename}
            onToggleHidden={onToggleHidden}
          />
        ))}
    </div>
  )
}
