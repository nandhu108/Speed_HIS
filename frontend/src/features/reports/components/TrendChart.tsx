import type { TrendPoint } from '../types'

const WIDTH = 640
const HEIGHT = 180
const PAD = { top: 16, right: 16, bottom: 28, left: 56 }
const GRID_COLOR = 'var(--color-border-soft)'
const AXIS_COLOR = 'var(--color-ink-faint)'

/** Single-series daily trend line — same shape as EMR's ResultsTrendChart (thin 2px line, rounded points, native tooltips, table fallback); no legend needed for a single series. */
export function TrendChart({ points, valuePrefix = '' }: { points: TrendPoint[]; valuePrefix?: string }) {
  if (points.length === 0) {
    return <p className="py-6 text-center text-sm text-ink-faint">No data for this range.</p>
  }

  const values = points.map((p) => p.amount)
  const minValue = Math.min(0, ...values)
  const maxValue = Math.max(...values)
  const valueRange = maxValue - minValue || 1
  const yPad = valueRange * 0.1

  const plotWidth = WIDTH - PAD.left - PAD.right
  const plotHeight = HEIGHT - PAD.top - PAD.bottom
  const x = (i: number) => PAD.left + (points.length === 1 ? plotWidth / 2 : (i / (points.length - 1)) * plotWidth)
  const y = (v: number) => PAD.top + plotHeight - ((v - (minValue - yPad)) / (valueRange + yPad * 2)) * plotHeight

  const linePath = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${x(i).toFixed(1)} ${y(p.amount).toFixed(1)}`).join(' ')
  const areaPath = `${linePath} L ${x(points.length - 1).toFixed(1)} ${(PAD.top + plotHeight).toFixed(1)} L ${x(0).toFixed(1)} ${(PAD.top + plotHeight).toFixed(1)} Z`
  const gridLines = [0, 0.5, 1].map((t) => PAD.top + plotHeight * t)

  return (
    <div>
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} className="w-full" role="img" aria-label="Daily trend">
        <defs>
          <linearGradient id="trend-area" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="var(--color-indigo)" stopOpacity="0.28" />
            <stop offset="1" stopColor="var(--color-indigo)" stopOpacity="0" />
          </linearGradient>
        </defs>
        {gridLines.map((gy) => (
          <line key={gy} x1={PAD.left} y1={gy} x2={WIDTH - PAD.right} y2={gy} stroke={GRID_COLOR} strokeWidth={1} />
        ))}
        <text x={PAD.left - 8} y={y(maxValue)} textAnchor="end" fontSize={9} fill={AXIS_COLOR}>{valuePrefix}{Math.round(maxValue).toLocaleString()}</text>
        <text x={PAD.left - 8} y={y(minValue) + 3} textAnchor="end" fontSize={9} fill={AXIS_COLOR}>{valuePrefix}{Math.round(minValue).toLocaleString()}</text>
        <path d={areaPath} fill="url(#trend-area)" />
        <path d={linePath} fill="none" stroke="var(--color-indigo)" strokeWidth={2.5} strokeLinecap="round" strokeLinejoin="round" />
        {points.map((p, i) => (
          <circle key={`${p.date}-${i}`} cx={x(i)} cy={y(p.amount)} r={3.5} fill="var(--color-indigo)" stroke="var(--color-panel-solid)" strokeWidth={1.5}>
            <title>{p.date}: {valuePrefix}{p.amount.toLocaleString()}</title>
          </circle>
        ))}
      </svg>
      <div className="mt-1 flex items-center justify-between text-xs text-ink-faint">
        <span>{points[0].date}</span>
        <span>{points[points.length - 1].date}</span>
      </div>

      <details className="mt-2">
        <summary className="cursor-pointer text-xs text-accent-2 hover:underline">View as table</summary>
        <table className="mt-2 w-full text-xs">
          <thead>
            <tr className="border-b border-border-soft text-left text-ink-faint">
              <th className="py-1 pr-2">Date</th>
              <th className="py-1">Amount</th>
            </tr>
          </thead>
          <tbody>
            {points.map((p, i) => (
              <tr key={`${p.date}-${i}`} className="border-b border-border-soft last:border-0">
                <td className="py-1 pr-2 text-ink-muted">{p.date}</td>
                <td className="py-1 text-ink">{valuePrefix}{p.amount.toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </div>
  )
}
