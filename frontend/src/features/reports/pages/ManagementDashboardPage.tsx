import { AlertTriangle, Bed, BedDouble, IndianRupee, Search, Users, Wallet } from 'lucide-react'
import { useMemo, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { Spinner } from '../../../components/ui/Spinner'
import { defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { useAuth } from '../../auth/AuthContext'
import { useBillingDashboard, useIpDashboard, useManagementDashboard, useOpDashboard, useRevenueDashboard } from '../api'
import { Donut, DualTrendChart, Sparkline } from '../components/DashboardCharts'
import { DONUT_COLORS, inr, inrCompact } from '../components/dashboardFormat'
import type { BranchSummaryRow } from '../types'
import '../dashboard.css'

const PRESETS = [7, 30, 90] as const

const fmtDate = (iso: string) =>
  iso ? new Date(`${iso}T00:00:00`).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).replace(/ /g, '-') : ''

const daysBetween = (from: string, to: string) =>
  Math.max(1, Math.round((new Date(`${to}T00:00:00`).getTime() - new Date(`${from}T00:00:00`).getTime()) / 86_400_000) + 1)

function Kpi(props: {
  label: string
  value: ReactNode
  icon: ReactNode
  accent: string
  soft: string
  chip?: ReactNode
  visual?: ReactNode
  foot?: string
  alert?: boolean
}) {
  return (
    <div className="dsh-kpi" style={{ ['--accent' as string]: props.accent, ['--soft' as string]: props.soft }}>
      <div className="dsh-kpiTop">
        <span className="dsh-kpiIcon">{props.icon}</span>
        <small className="lbl">{props.label}</small>
      </div>
      <div className={`dsh-kpiVal dsh-display${props.alert ? ' alert' : ''}`}>{props.value}</div>
      {props.chip ?? <span className="dsh-chip flat" style={{ visibility: 'hidden' }}>—</span>}
      {props.visual ?? <div className="dsh-sparkGap" />}
      {props.foot && <div className="dsh-kpiFoot">{props.foot}</div>}
    </div>
  )
}

type SortKey = 'branchName' | 'revenue' | 'collection' | 'opVisitCount' | 'activeInpatients'

export function ManagementDashboardPage() {
  const navigate = useNavigate()
  const { user, hasPermission } = useAuth()
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const range = { fromDate, toDate }

  const { data, isLoading } = useManagementDashboard(range)
  const { data: revenue } = useRevenueDashboard(range, hasPermission('DASHBOARD.REVENUE.VIEW'))
  const { data: billing } = useBillingDashboard(range, hasPermission('DASHBOARD.BILLING.VIEW'))
  const { data: op } = useOpDashboard(range, hasPermission('DASHBOARD.OP.VIEW'))
  const { data: ip } = useIpDashboard(range, hasPermission('DASHBOARD.IP.VIEW'))

  const [branchQuery, setBranchQuery] = useState('')
  const [sort, setSort] = useState<{ key: SortKey; dir: 1 | -1 }>({ key: 'revenue', dir: -1 })

  const days = daysBetween(fromDate, toDate)
  // The OP/IP/Billing/Revenue dashboards are always scoped to the signed-in branch, while the
  // management KPIs can be cross-branch — so in a cross-branch view their numbers are tagged with
  // the branch they belong to and kept out of the all-branch KPI cards.
  const cross = !!data?.isCrossBranch
  const local = cross ? undefined : true
  const branchTag = cross && user?.branchName ? <span className="dsh-pill">{user.branchName}</span> : null
  const activePreset = PRESETS.find((p) => fromDate === defaultDateRange(p).fromDate && toDate === defaultDateRange(p).toDate)

  const branches = useMemo(() => {
    const q = branchQuery.trim().toLowerCase()
    const rows = (data?.byBranch ?? []).filter((b) => b.branchName.toLowerCase().includes(q))
    return [...rows].sort((a, b) => {
      const av = a[sort.key]
      const bv = b[sort.key]
      return (typeof av === 'string' ? av.localeCompare(bv as string) : (av as number) - (bv as number)) * sort.dir
    })
  }, [data?.byBranch, branchQuery, sort])

  const totals = useMemo(
    () =>
      (data?.byBranch ?? []).reduce(
        (t, b) => ({
          revenue: t.revenue + b.revenue,
          collection: t.collection + b.collection,
          op: t.op + b.opVisitCount,
          ip: t.ip + b.activeInpatients,
        }),
        { revenue: 0, collection: 0, op: 0, ip: 0 },
      ),
    [data?.byBranch],
  )

  const sortBy = (key: SortKey) =>
    setSort((s) => (s.key === key ? { key, dir: (s.dir * -1) as 1 | -1 } : { key, dir: key === 'branchName' ? 1 : -1 }))
  const sortMark = (key: SortKey) => (sort.key === key ? (sort.dir === -1 ? ' ▾' : ' ▴') : '')
  const headers: { key: SortKey; label: string }[] = [
    { key: 'branchName', label: 'Branch' },
    { key: 'revenue', label: 'Revenue' },
    { key: 'collection', label: 'Collection' },
    { key: 'opVisitCount', label: 'OP Visits' },
    { key: 'activeInpatients', label: 'Active Inpatients' },
  ]

  const deptRows = useMemo(() => {
    const sorted = [...(revenue?.byDepartment ?? [])].filter((d) => d.amount > 0).sort((a, b) => b.amount - a.amount)
    const top = sorted.slice(0, 6).map((d) => ({ label: d.departmentName, value: d.amount }))
    const rest = sorted.slice(6).reduce((a, d) => a + d.amount, 0)
    return rest > 0 ? [...top, { label: 'Others', value: rest }] : top
  }, [revenue?.byDepartment])
  const deptTotal = deptRows.reduce((a, d) => a + d.value, 0)
  const top3Share = deptTotal > 0 ? Math.round((deptRows.slice(0, 3).reduce((a, d) => a + d.value, 0) / deptTotal) * 100) : 0

  const collectedPct = data && data.totalRevenue > 0 ? Math.round((data.totalCollection / data.totalRevenue) * 100) : null
  const topDoctors = [...(op?.byDoctor ?? [])].sort((a, b) => b.visitCount - a.visitCount).slice(0, 5)
  const topDoctorMax = Math.max(1, ...topDoctors.map((d) => d.visitCount))
  const modes = [...(billing?.byMode ?? [])].sort((a, b) => b.amount - a.amount)

  return (
    <div className="dsh">
      <div className="dsh-tabs">
        <span className="dsh-miniTab">
          <i>◈</i> Dashboard
        </span>
        <span className="dsh-scopeNote">
          {data?.isCrossBranch ? 'All branches (you hold cross-branch reporting access)' : 'Your branch'}
        </span>
        {hasPermission('PATIENT.PATIENT.CREATE') && (
          <button type="button" className="dsh-linkBtn" onClick={() => navigate('/patients/new')}>
            ＋ New Registration
          </button>
        )}
      </div>

      <section className="dsh-hero">
        <div className="dsh-heroTop">
          <div className="dsh-heroTitle">
            <h1 className="dsh-display">
              Hospital <span className="dsh-gold">Dashboard</span>
            </h1>
            <p>
              <span className="dsh-scopePill">{data?.isCrossBranch ? 'ALL BRANCHES' : 'YOUR BRANCH'}</span>
              <span>
                {fmtDate(fromDate)} → {fmtDate(toDate)} • {days} days
              </span>
            </p>
          </div>
          <div className="dsh-dateBar">
            <div className="dsh-presets" role="group" aria-label="Quick range">
              {PRESETS.map((p) => (
                <button
                  key={p}
                  type="button"
                  className={activePreset === p ? 'on' : ''}
                  aria-pressed={activePreset === p}
                  onClick={() => setRange(defaultDateRange(p))}
                >
                  {p}D
                </button>
              ))}
            </div>
            <div className="dsh-dateField">
              <label htmlFor="dsh-from">FROM</label>
              <input
                id="dsh-from"
                type="date"
                value={fromDate}
                max={toDate}
                onChange={(e) => e.target.value && setRange((r) => ({ ...r, fromDate: e.target.value }))}
              />
            </div>
            <div className="dsh-dateField">
              <label htmlFor="dsh-to">TO</label>
              <input
                id="dsh-to"
                type="date"
                value={toDate}
                min={fromDate}
                onChange={(e) => e.target.value && setRange((r) => ({ ...r, toDate: e.target.value }))}
              />
            </div>
          </div>
        </div>
      </section>

      {isLoading || !data ? (
        <div className="py-10">
          <Spinner />
        </div>
      ) : (
        <>
          <section className="dsh-kpis">
            <Kpi
              label="Revenue"
              value={inr(data.totalRevenue)}
              icon={<Wallet className="size-4" aria-hidden />}
              accent="#2348ff"
              soft="#e9edff"
              visual={local && revenue && <Sparkline values={revenue.dailyTrend.map((p) => p.amount)} color="#2348ff" />}
              foot={data.isCrossBranch ? 'Billed across all branches' : 'Billed in this branch'}
            />
            <Kpi
              label="Collection"
              value={inr(data.totalCollection)}
              icon={<IndianRupee className="size-4" aria-hidden />}
              accent="#0eb7a2"
              soft="#d9f9f1"
              chip={collectedPct !== null ? <span className="dsh-chip">{collectedPct}% of billed</span> : undefined}
              visual={local && billing && <Sparkline values={billing.dailyTrend.map((p) => p.amount)} color="#14b8a6" />}
              foot={local && billing ? `Outstanding ${inr(billing.outstandingAmount)}` : 'Collected in range'}
            />
            <Kpi
              label="OP Visits"
              value={data.opVisitCount.toLocaleString('en-IN')}
              icon={<Users className="size-4" aria-hidden />}
              accent="#7a5bff"
              soft="#ece7ff"
              visual={local && op && <Sparkline values={op.dailyTrend.map((p) => p.amount)} color="#7a5bff" />}
              foot={local && op ? `${op.newCount} new • ${op.reviewCount} review` : 'Visits in range'}
            />
            <Kpi
              label="Active Inpatients"
              value={data.activeInpatients.toLocaleString('en-IN')}
              icon={<BedDouble className="size-4" aria-hidden />}
              accent="#b98a1e"
              soft="#fdf3dc"
              foot={local && ip ? `${ip.admissionsInRange} admitted • ${ip.dischargesInRange} discharged` : 'Currently admitted'}
            />
            <Kpi
              label="Bed Occupancy"
              value={`${data.bedOccupancyPercent}%`}
              icon={<Bed className="size-4" aria-hidden />}
              accent="#0e9d8b"
              soft="#d9f9f1"
              chip={local && ip ? <span className="dsh-chip flat">▬ {ip.totalBeds} beds</span> : undefined}
              visual={
                <div className="dsh-sparkGap">
                  <div className="dsh-meter">
                    <i style={{ width: `${Math.min(100, Math.max(0, data.bedOccupancyPercent))}%` }} />
                  </div>
                </div>
              }
              foot={local && ip ? `${ip.occupied} occupied • ${ip.vacant} vacant` : 'Of active beds'}
            />
            <Kpi
              label="Unack. Alerts"
              value={data.unacknowledgedAlerts}
              icon={<AlertTriangle className="size-4" aria-hidden />}
              accent="#ff5470"
              soft="#ffe3e8"
              alert={data.unacknowledgedAlerts > 0}
              chip={
                data.unacknowledgedAlerts > 0 ? (
                  <span className="dsh-chip down">● needs action</span>
                ) : (
                  <span className="dsh-chip">● all clear</span>
                )
              }
              foot="Lab critical + ICU vitals alerts"
            />
          </section>

          <section className="dsh-charts">
            <div className="dsh-card">
              <div className="dsh-cardHead">
                <div>
                  <b>Revenue Trend</b> {branchTag}
                  <small>
                    {fmtDate(fromDate)} to {fmtDate(toDate)} • daily billed vs collected
                  </small>
                </div>
                <div className="dsh-legend">
                  <span>
                    <i style={{ background: '#2348ff' }} /> Billed
                  </span>
                  {billing && (
                    <span>
                      <i style={{ background: '#14b8a6' }} /> Collected
                    </span>
                  )}
                </div>
              </div>
              {revenue ? (
                <DualTrendChart
                  series={[
                    { label: 'Billed', color: '#2348ff', points: revenue.dailyTrend },
                    ...(billing ? [{ label: 'Collected', color: '#14b8a6', points: billing.dailyTrend }] : []),
                  ]}
                />
              ) : (
                <div className="dsh-empty">Revenue trend needs the Revenue dashboard permission.</div>
              )}
            </div>

            <div className="dsh-card">
              <div className="dsh-cardHead">
                <div>
                  <b>Revenue by Department</b> {branchTag}
                  <small>Share of billed • {days} days</small>
                </div>
              </div>
              {revenue ? (
                <>
                  <div className="dsh-donutWrap">
                    <Donut rows={deptRows} centre={inrCompact(deptTotal)} centreSub="billed" />
                    {deptTotal > 0 && (
                      <div className="dsh-donutLegend">
                        {deptRows.map((d, i) => (
                          <div className="row" key={d.label}>
                            <i style={{ background: DONUT_COLORS[i % DONUT_COLORS.length] }} />
                            <span title={`${d.label} • ${inr(d.value)}`}>{d.label}</span>
                            <b>{Math.round((d.value / deptTotal) * 100)}%</b>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                  {deptRows.length > 3 && (
                    <div className="dsh-donutNote">
                      <div className="dsh-bar">
                        <i style={{ width: `${top3Share}%` }} />
                      </div>
                      <p>Top 3 departments drive {top3Share}% of billed revenue.</p>
                    </div>
                  )}
                </>
              ) : (
                <div className="dsh-empty">Needs the Revenue dashboard permission.</div>
              )}
            </div>
          </section>

          {data.byBranch.length > 0 && (
            <section className="dsh-card dsh-tableCard">
              <div className="dsh-tableTools">
                <div>
                  <b>By Branch</b>
                  <small>Branch-wise performance for the selected range</small>
                </div>
                <div className="dsh-search">
                  <Search className="size-3.5" aria-hidden />
                  <input
                    value={branchQuery}
                    onChange={(e) => setBranchQuery(e.target.value)}
                    placeholder="Search branch…"
                    aria-label="Search branch"
                  />
                </div>
              </div>
              <div className="dsh-tableScroll">
                <table>
                  <thead>
                    <tr>
                      {headers.map((h) => (
                        <th key={h.key} aria-sort={sort.key === h.key ? (sort.dir === 1 ? 'ascending' : 'descending') : undefined}>
                          <button type="button" onClick={() => sortBy(h.key)}>
                            {h.label}
                            {sortMark(h.key)}
                          </button>
                        </th>
                      ))}
                      <th className="plain">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {branches.map((b: BranchSummaryRow) => {
                      const active = b.activeInpatients > 0 || b.opVisitCount > 0
                      return (
                        <tr key={b.branchId}>
                          <td>
                            <b>{b.branchName}</b>
                          </td>
                          <td className="dsh-num">{inr(b.revenue)}</td>
                          <td className="dsh-num">{inr(b.collection)}</td>
                          <td className="dsh-num">{b.opVisitCount.toLocaleString('en-IN')}</td>
                          <td className="dsh-num">{b.activeInpatients.toLocaleString('en-IN')}</td>
                          <td>
                            <span className={`dsh-status${active ? '' : ' dim'}`}>{active ? '● Active' : '○ Quiet'}</span>
                          </td>
                        </tr>
                      )
                    })}
                    {branches.length === 0 && (
                      <tr>
                        <td colSpan={6} style={{ textAlign: 'center', color: 'var(--d-faint)', padding: 26 }}>
                          No branches match “{branchQuery.trim()}”.
                        </td>
                      </tr>
                    )}
                  </tbody>
                  {data.byBranch.length > 1 && (
                    <tfoot>
                      <tr>
                        <td>
                          <b>Total • {data.byBranch.length} branches</b>
                        </td>
                        <td className="dsh-num">{inr(totals.revenue)}</td>
                        <td className="dsh-num">{inr(totals.collection)}</td>
                        <td className="dsh-num">{totals.op.toLocaleString('en-IN')}</td>
                        <td className="dsh-num">{totals.ip.toLocaleString('en-IN')}</td>
                        <td />
                      </tr>
                    </tfoot>
                  )}
                </table>
              </div>
            </section>
          )}

          {(billing || ip || op) && (
            <section className="dsh-bottom">
              {billing && (
                <div className="dsh-card dsh-pad">
                  <h4>Payments split {branchTag}</h4>
                  <p className="sub">Collection by payment mode for the selected range</p>
                  {modes.length === 0 && <p className="dsh-note">No payments in this range.</p>}
                  {modes.map((m) => (
                    <div className="dsh-payRow" key={m.paymentModeId}>
                      <span>
                        {m.paymentModeName} • {m.txnCount} txn
                      </span>
                      <b>{inr(m.amount)}</b>
                    </div>
                  ))}
                  <div className="dsh-payRow">
                    <span>Outstanding (unpaid bills)</span>
                    <b style={{ color: billing.outstandingAmount > 0 ? 'var(--d-rose)' : undefined }}>{inr(billing.outstandingAmount)}</b>
                  </div>
                </div>
              )}

              {ip && (
                <div className="dsh-card dsh-pad">
                  <h4>
                    Bed status {branchTag ?? <span className="dsh-pill">{ip.occupancyRatePercent}% occupied</span>}
                  </h4>
                  <p className="sub">
                    {ip.totalBeds} beds in service • {ip.occupancyRatePercent}% occupied
                  </p>
                  {[
                    { n: 'Occupied', v: ip.occupied, c: 'linear-gradient(90deg,#ff9f6b,#ff5470)' },
                    { n: 'Vacant', v: ip.vacant, c: 'linear-gradient(90deg,#2dd4bf,#2348ff)' },
                  ].map((r) => (
                    <div className="dsh-barRow" key={r.n}>
                      <b>{r.n}</b>
                      <div className="track">
                        <i style={{ width: `${ip.totalBeds ? (r.v / ip.totalBeds) * 100 : 0}%`, background: r.c }} />
                      </div>
                      <span>{r.v}</span>
                    </div>
                  ))}
                  <div className="dsh-payRow">
                    <span>Admissions in range</span>
                    <b>{ip.admissionsInRange}</b>
                  </div>
                  <div className="dsh-payRow">
                    <span>Discharges in range</span>
                    <b>{ip.dischargesInRange}</b>
                  </div>
                  <div className="dsh-payRow">
                    <span>Average length of stay</span>
                    <b>{ip.averageLengthOfStayDays != null ? `${ip.averageLengthOfStayDays.toFixed(1)} days` : '—'}</b>
                  </div>
                </div>
              )}

              {op && (
                <div className="dsh-card dsh-pad">
                  <h4>
                    Top doctors by OP visits {branchTag ?? <span className="dsh-pill">{op.totalVisits} visits</span>}
                  </h4>
                  <p className="sub">
                    {op.totalVisits} visits • {op.newCount} new • {op.reviewCount} review in range
                  </p>
                  {topDoctors.length === 0 && <p className="dsh-note">No OP visits in this range.</p>}
                  {topDoctors.map((d, i) => (
                    <div className="dsh-barRow" key={d.doctorId}>
                      <b title={d.doctorName}>{d.doctorName}</b>
                      <div className="track">
                        <i
                          style={{
                            width: `${(d.visitCount / topDoctorMax) * 100}%`,
                            background: DONUT_COLORS[i % DONUT_COLORS.length],
                          }}
                        />
                      </div>
                      <span>{d.visitCount}</span>
                    </div>
                  ))}
                </div>
              )}
            </section>
          )}

          {!data.isCrossBranch && (
            <p className="dsh-note">Single-branch view — grant REPORT.CROSSBRANCH.VIEW to see every branch here.</p>
          )}
        </>
      )}
    </div>
  )
}
