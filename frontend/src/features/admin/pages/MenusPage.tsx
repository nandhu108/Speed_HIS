import { GripVertical, Search } from 'lucide-react'
import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { IconPicker } from '../../../components/ui/IconPicker'
import { Input } from '../../../components/ui/Input'
import { MenuIconFor } from '../../../components/ui/menuIcons'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteMenu, useMenuTree, usePermissionCatalogue, useSaveMenu, type SaveMenuPayload } from '../api'
import type { MenuNodeAdmin } from '../types'

interface EditorState extends SaveMenuPayload {
  id?: number
}

function nodeMatches(node: MenuNodeAdmin, query: string): boolean {
  if (node.title.toLowerCase().includes(query)) return true
  return node.children.some((child) => nodeMatches(child, query))
}

function filterTree(nodes: MenuNodeAdmin[], query: string): MenuNodeAdmin[] {
  if (!query) return nodes
  return nodes
    .filter((node) => nodeMatches(node, query))
    .map((node) => (node.children.length ? { ...node, children: filterTree(node.children, query) } : node))
}

/** Finds the sibling array a node with the given parentId lives in, within the real (unfiltered) tree. */
function findSiblings(tree: MenuNodeAdmin[], parentId: number | null): MenuNodeAdmin[] {
  if (parentId === null) return tree
  for (const node of tree) {
    if (node.id === parentId) return node.children
    const nested = findSiblings(node.children, parentId)
    if (nested.length > 0) return nested
  }
  return []
}

export function MenusPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('ADMIN.MENU.MANAGE')
  const { data: tree = [], isLoading } = useMenuTree()
  const { data: permissions = [] } = usePermissionCatalogue()
  const saveMenu = useSaveMenu()
  const deleteMenu = useDeleteMenu()

  const [search, setSearch] = useState('')
  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [draggedId, setDraggedId] = useState<number | null>(null)
  const [dragOverId, setDragOverId] = useState<number | null>(null)
  const [reordering, setReordering] = useState(false)

  const flat: { id: number; title: string }[] = []
  const flatten = (nodes: MenuNodeAdmin[], depth: number) => {
    for (const node of nodes) {
      flat.push({ id: node.id, title: `${'— '.repeat(depth)}${node.title}` })
      flatten(node.children, depth + 1)
    }
  }
  flatten(tree, 0)

  const submit = async () => {
    if (!editor) return
    setError(null)
    try {
      await saveMenu.mutateAsync(editor)
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const dragEnabled = canManage && search.trim() === ''

  const handleDrop = async (target: MenuNodeAdmin) => {
    const draggedNode = draggedId !== null ? findNodeById(tree, draggedId) : null
    setDraggedId(null)
    setDragOverId(null)
    if (!draggedNode || draggedNode.id === target.id) return

    if (draggedNode.parentId === target.parentId) {
      // Same section: reorder among siblings.
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
              : saveMenu.mutateAsync({
                  id: sibling.id,
                  parentId: sibling.parentId,
                  title: sibling.title,
                  icon: sibling.icon,
                  path: sibling.path,
                  permissionCode: sibling.permissionCode,
                  sortOrder: (i + 1) * 10,
                  isActive: sibling.isActive,
                  hiddenByDefault: sibling.hiddenByDefault,
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

    // Different section: nest the dragged item inside the drop target.
    if (containsNode(draggedNode, target.id)) {
      window.alert("Can't move a section into one of its own children.")
      return
    }

    const newSiblings = target.children
    const nextSortOrder = newSiblings.length > 0 ? Math.max(...newSiblings.map((s) => s.sortOrder)) + 10 : 10

    setReordering(true)
    try {
      await saveMenu.mutateAsync({
        id: draggedNode.id,
        parentId: target.id,
        title: draggedNode.title,
        icon: draggedNode.icon,
        path: draggedNode.path,
        permissionCode: draggedNode.permissionCode,
        sortOrder: nextSortOrder,
        isActive: draggedNode.isActive,
        hiddenByDefault: draggedNode.hiddenByDefault,
      })
    } catch (err) {
      window.alert(errorMessage(err))
    } finally {
      setReordering(false)
    }
  }

  const visibleTree = filterTree(tree, search.trim().toLowerCase())

  const renderNode = (node: MenuNodeAdmin, depth: number) => (
    <div key={node.id}>
      <div
        draggable={dragEnabled}
        onDragStart={(e) => {
          e.dataTransfer.effectAllowed = 'move'
          setDraggedId(node.id)
        }}
        onDragOver={(e) => {
          if (!dragEnabled || draggedId === null) return
          e.preventDefault()
          setDragOverId(node.id)
        }}
        onDragLeave={() => setDragOverId((id) => (id === node.id ? null : id))}
        onDrop={(e) => {
          e.preventDefault()
          void handleDrop(node)
        }}
        onDragEnd={() => {
          setDraggedId(null)
          setDragOverId(null)
        }}
        className={`flex flex-wrap items-center justify-between gap-2 rounded-shell-sm px-3 py-2 hover:bg-panel-2 ${
          depth === 0 ? 'bg-panel-2/60' : ''
        } ${draggedId === node.id ? 'opacity-40' : ''} ${
          dragOverId === node.id && draggedId !== node.id ? 'ring-2 ring-accent-2/60' : ''
        }`}
        style={{ marginLeft: depth * 20 }}
      >
        <div className="flex items-center gap-2 text-sm">
          {dragEnabled && <GripVertical className="size-3.5 shrink-0 cursor-grab text-ink-faint active:cursor-grabbing" aria-hidden />}
          <span className="flex size-6 items-center justify-center rounded-[6px] bg-panel-2 text-ink-muted">
            <MenuIconFor name={node.icon} />
          </span>
          <span className={`text-ink ${depth === 0 ? 'font-bold' : 'font-medium'}`}>{node.title}</span>
          {node.path && <code className="rounded-[4px] bg-panel-2 px-1.5 py-0.5 font-mono text-xs text-ink-muted">{node.path}</code>}
          {node.permissionCode && (
            <code className="rounded-[4px] bg-accent-2/10 px-1.5 py-0.5 font-mono text-xs text-accent-2">{node.permissionCode}</code>
          )}
          {!node.isActive && <StatusBadge tone="neutral">Hidden</StatusBadge>}
        </div>
        {canManage && (
          <div className="flex gap-1">
            <Button
              variant="ghost"
              onClick={() =>
                setEditor({
                  id: node.id,
                  parentId: node.parentId,
                  title: node.title,
                  icon: node.icon,
                  path: node.path,
                  permissionCode: node.permissionCode,
                  sortOrder: node.sortOrder,
                  isActive: node.isActive,
                  hiddenByDefault: node.hiddenByDefault,
                })
              }
            >
              Edit
            </Button>
            <Button
              variant="ghost"
              className="text-crit"
              onClick={async () => {
                if (window.confirm(`Delete menu '${node.title}'?`)) {
                  try {
                    await deleteMenu.mutateAsync(node.id)
                  } catch (err) {
                    window.alert(errorMessage(err))
                  }
                }
              }}
            >
              Delete
            </Button>
          </div>
        )}
      </div>
      {node.children.map((child) => renderNode(child, depth + 1))}
    </div>
  )

  return (
    <>
      <PageHeader
        title="Menus"
        subtitle="The sidebar is data-driven, organized by module — menus without a permission are visible to everyone. Drag the handle to reorder within a section, or drop onto another section to move into it."
        actions={
          canManage && (
            <Button
              onClick={() =>
                setEditor({
                  parentId: null, title: '', icon: null, path: null, permissionCode: null,
                  sortOrder: 0, isActive: true, hiddenByDefault: false,
                })
              }
            >
              New menu
            </Button>
          )
        }
      />

      <div className="mb-4 max-w-sm">
        <div className="relative">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-ink-faint" aria-hidden />
          <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search menus…" inputClassName="pl-9" />
        </div>
        {search.trim() !== '' && canManage && (
          <p className="mt-1.5 text-[11px] text-ink-faint">Clear the search to drag and reorder items.</p>
        )}
      </div>

      <div className="relative rounded-shell border border-border-soft bg-panel p-2 shadow-shell-sm">
        {reordering && (
          <div className="absolute inset-0 z-10 flex items-center justify-center rounded-shell bg-panel/70 backdrop-blur-sm">
            <Spinner label="Saving new order…" />
          </div>
        )}
        {isLoading ? (
          <Spinner />
        ) : visibleTree.length === 0 ? (
          <p className="py-10 text-center text-sm text-ink-faint">No menus match "{search}".</p>
        ) : (
          visibleTree.map((node) => renderNode(node, 0))
        )}
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit menu' : 'New menu'}
          onClose={() => setEditor(null)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setEditor(null)}>
                Cancel
              </Button>
              <Button onClick={submit} loading={saveMenu.isPending}>
                Save
              </Button>
            </>
          }
        >
          <div className="grid grid-cols-2 gap-3">
            {error && <p className="col-span-2 rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
            <Input
              label="Title"
              className="col-span-2"
              value={editor.title}
              onChange={(e) => setEditor({ ...editor, title: e.target.value })}
            />
            <Select
              label="Parent"
              placeholder="— Top level —"
              options={flat.filter((item) => item.id !== editor.id).map((item) => ({ value: item.id, label: item.title }))}
              value={editor.parentId?.toString() ?? ''}
              onChange={(e) => setEditor({ ...editor, parentId: e.target.value ? Number(e.target.value) : null })}
            />
            <IconPicker value={editor.icon} onChange={(icon) => setEditor({ ...editor, icon })} />
            <Input
              label="Path (empty = group)"
              value={editor.path ?? ''}
              onChange={(e) => setEditor({ ...editor, path: e.target.value || null })}
            />
            <Select
              label="Required permission"
              placeholder="— Everyone —"
              options={permissions.map((p) => ({ value: p.code, label: p.code }))}
              value={editor.permissionCode ?? ''}
              onChange={(e) => setEditor({ ...editor, permissionCode: e.target.value || null })}
            />
            <Input
              label="Sort order"
              type="number"
              value={editor.sortOrder}
              onChange={(e) => setEditor({ ...editor, sortOrder: Number(e.target.value) })}
            />
            <label className="flex items-end gap-2 pb-2 text-sm text-ink">
              <input
                type="checkbox"
                checked={editor.isActive}
                onChange={(e) => setEditor({ ...editor, isActive: e.target.checked })}
              />
              Visible
            </label>
            <label className="flex items-end gap-2 pb-2 text-sm text-ink">
              <input
                type="checkbox"
                checked={editor.hiddenByDefault}
                onChange={(e) => setEditor({ ...editor, hiddenByDefault: e.target.checked })}
              />
              Hidden by default (roles opt in via "By menu")
            </label>
          </div>
        </Modal>
      )}
    </>
  )
}

function findNodeById(nodes: MenuNodeAdmin[], id: number): MenuNodeAdmin | null {
  for (const node of nodes) {
    if (node.id === id) return node
    const found = findNodeById(node.children, id)
    if (found) return found
  }
  return null
}

/** True if `id` is `node` itself or anywhere in its subtree — used to block dropping a section into its own descendant. */
function containsNode(node: MenuNodeAdmin, id: number): boolean {
  if (node.id === id) return true
  return node.children.some((child) => containsNode(child, id))
}
