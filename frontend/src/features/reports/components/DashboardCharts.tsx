import { useId, useState } from 'react'
import type { TrendPoint } from '../types'
import { DONUT_COLORS, inr, inrCompact } from './dashboardFormat'

const longDate = (iso: string) =>
  new Date(`${iso.slice(0, 10)}T00:00:00`).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })

const shortDate = (iso: string) => {
  const [, m, d] = iso.slice(0, 10).split('-')
  return `${Number(d)}/${Number(m)}`
}

/** Smooth (monotone-ish) path through points using mid-point cubic curves, as in the design. */
function smoothPath(pts: [number, number][]): string {
  if (pts.length === 0) return ''
  let p = `M ${pts[0][0].toFixed(1)} ${pts[0][1].toFixed(1)}`
  for (let i = 1; i < pts.length; i++) {
    const [x0, y0] = pts[i - 1]
    const [x1, y1] = pts[i]
    const xm = (x0 + x1) / 2
    p += ` C ${xm.toFixed(1)} ${y0.toFixed(1)}, ${xm.toFixed(1)} ${y1.toFixed(1)}, ${x1.toFixed(1)} ${y1.toFixed(1)}`
  }
  return p
}

/** Small filled sparkline for KPI cards. Renders nothing for fewer than two points. */
export function Sparkline({ values, color }: { values: number[]; color: string }) {
  const gid = useId()
  if (values.length < 2) return null
  const W = 220
  const H = 34
  const min = Math.min(...values)
  const max = Math.max(...values)
  const rg = max - min || 1
  const pts = values.map((v, i) => [(i / (values.length - 1)) * W, H - 4 - ((v - min) / rg) * (H - 9)] as [number, number])
  const line = smoothPath(pts)
  const last = pts[pts.length - 1]
  return (
    <svg className="dsh-spark" viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="none" aria-hidden>
      <defs>
        <linearGradient id={gid} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor={color} stopOpacity="0.27" />
          <stop offset="1" stopColor={color} stopOpacity="0" />
        </linearGradient>
      </defs>
      <path d={`${line} L ${W} ${H} L 0 ${H} Z`} fill={`url(#${gid})`} />
      <path d={line} fill="none" stroke={color} strokeWidth={2} strokeLinejoin="round" vectorEffect="non-scaling-stroke" />
      <circle cx={last[0] - 1} cy={last[1]} r={3} fill={color} />
    </svg>
  )
}

interface Series {
  label: string
  color: string
  points: TrendPoint[]
}

/**
 * Two-series daily trend (e.g. billed vs collected) with a hover readout. Series are merged on
 * date, so a day present in only one series reads as 0 in the other.
 */
export function DualTrendChart({ series, height = 260 }: { series: Series[]; height?: number }) {
  const gid = useId()
  const [hover, setHover] = useState<number | null>(null)
  const dates = Array.from(new Set(series.flatMap((s) => s.points.map((p) => p.date.slice(0, 10))))).sort()
  if (dates.length === 0) {
    return <div className="dsh-empty">No data for this range.</div>
  }
  const lookup = series.map((s) => new Map(s.points.map((p) => [p.date.slice(0, 10), p.amount])))
  const data = series.map((_, si) => dates.map((d) => lookup[si].get(d) ?? 0))

  const W = 760
  const H = height
  const padL = 48
  const padR = 10
  const padT = 12
  const padB = 26
  const max = Math.max(1, ...data.flat()) * 1.15
  const n = dates.length
  const X = (i: number) => padL + (n === 1 ? (W - padL - padR) / 2 : ((W - padL - padR) * i) / (n - 1))
  const Y = (v: number) => padT + (H - padT - padB) * (1 - v / max)
  const grid = [0, 1, 2, 3].map((g) => (max * g) / 3)
  const labelStep = Math.max(1, Math.ceil(n / 7))

  const onMove = (e: React.MouseEvent<SVGSVGElement>) => {
    const r = e.currentTarget.getBoundingClientRect()
    const mx = ((e.clientX - r.left) / r.width) * W
    const i = n === 1 ? 0 : Math.round(((mx - padL) / (W - padL - padR)) * (n - 1))
    setHover(Math.max(0, Math.min(n - 1, i)))
  }

  return (
    <div className="dsh-chartBox">
      <svg
        viewBox={`0 0 ${W} ${H}`}
        className="dsh-trend"
        role="img"
        aria-label={`Daily ${series.map((s) => s.label).join(' vs ')}`}
        onMouseMove={onMove}
        onMouseLeave={() => setHover(null)}
      >
        <defs>
          <linearGradient id={gid} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor={series[0].color} stopOpacity="0.24" />
            <stop offset="1" stopColor={series[0].color} stopOpacity="0" />
          </linearGradient>
        </defs>
        {grid.map((v) => (
          <g key={v}>
            <line x1={padL} y1={Y(v)} x2={W - 4} y2={Y(v)} className="dsh-grid" />
            <text x={6} y={Y(v) + 3} className="dsh-axis">
              {v >= 1000 ? inrCompact(v).replace('₹', '') : Math.round(v)}
            </text>
          </g>
        ))}
        {data.map((vals, si) => {
          const pts = vals.map((v, i) => [X(i), Y(v)] as [number, number])
          const line = smoothPath(pts)
          return (
            <g key={series[si].label}>
              {si === 0 && n > 1 && (
                <path d={`${line} L ${X(n - 1)} ${H - padB} L ${X(0)} ${H - padB} Z`} fill={`url(#${gid})`} />
              )}
              <path d={line} fill="none" stroke={series[si].color} strokeWidth={2.4} strokeLinejoin="round" />
              {n === 1 && <circle cx={pts[0][0]} cy={pts[0][1]} r={4} fill={series[si].color} />}
            </g>
          )
        })}
        {dates.map((d, i) =>
          i % labelStep === 0 ? (
            <text key={d} x={X(i)} y={H - 8} textAnchor="middle" className="dsh-axis">
              {shortDate(d)}
            </text>
          ) : null,
        )}
        {hover !== null && (
          <g>
            <line x1={X(hover)} y1={padT} x2={X(hover)} y2={H - padB} className="dsh-hoverLine" />
            {data.map((vals, si) => (
              <circle key={si} cx={X(hover)} cy={Y(vals[hover])} r={4} fill={series[si].color} stroke="#fff" strokeWidth={2} />
            ))}
          </g>
        )}
      </svg>
      {hover !== null && (
        <div className="dsh-tip" style={{ left: `${Math.min(70, Math.max(6, (X(hover) / W) * 100))}%` }}>
          <b>{longDate(dates[hover])}</b>
          {series.map((s, si) => (
            <span key={s.label}>
              {' '}
              • {inr(data[si][hover])} {s.label.toLowerCase()}
            </span>
          ))}
        </div>
      )}
    </div>
  )
}

/** Ring chart with a centre total; slices beyond the palette fold into "Others". */
export function Donut({ rows, centre, centreSub }: { rows: { label: string; value: number }[]; centre: string; centreSub: string }) {
  const S = 168
  const c = S / 2
  const R = c - 6
  const r = R * 0.62
  const total = rows.reduce((a, x) => a + x.value, 0)
  if (total <= 0) {
    return <div className="dsh-empty">No billed revenue in this range.</div>
  }
  let a = -Math.PI / 2
  const gap = rows.length > 1 ? 0.02 : 0
  const arcs = rows.map((row, i) => {
    const a2 = a + (row.value / total) * Math.PI * 2
    const s = a + gap
    const e = a2 - gap
    const large = e - s > Math.PI ? 1 : 0
    const p = (rad: number, rr: number) => `${(c + rr * Math.cos(rad)).toFixed(2)} ${(c + rr * Math.sin(rad)).toFixed(2)}`
    const d =
      rows.length === 1
        ? `M ${c} ${c - R} A ${R} ${R} 0 1 1 ${c - 0.01} ${c - R} Z M ${c} ${c - r} A ${r} ${r} 0 1 0 ${c + 0.01} ${c - r} Z`
        : `M ${p(s, R)} A ${R} ${R} 0 ${large} 1 ${p(e, R)} L ${p(e, r)} A ${r} ${r} 0 ${large} 0 ${p(s, r)} Z`
    a = a2
    return <path key={row.label} d={d} fill={DONUT_COLORS[i % DONUT_COLORS.length]} fillRule="evenodd" />
  })
  return (
    <svg viewBox={`0 0 ${S} ${S}`} className="dsh-donut" role="img" aria-label="Revenue share by department">
      {arcs}
      <text x={c} y={c + 2} textAnchor="middle" className="dsh-donutVal">
        {centre}
      </text>
      <text x={c} y={c + 18} textAnchor="middle" className="dsh-donutSub">
        {centreSub}
      </text>
    </svg>
  )
}
