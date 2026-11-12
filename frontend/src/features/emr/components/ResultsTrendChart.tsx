import type { ResultsTrendSeries } from '../types'

const SERIES_COLOR = '#2a78d6'
const FLAG_COLOR: Record<string, string> = {
  Critical: '#d03b3b',
  High: '#fab219',
  Low: '#fab219',
  Normal: SERIES_COLOR,
}

const WIDTH = 640
const HEIGHT = 180
const PAD = { top: 16, right: 16, bottom: 28, left: 44 }

/** Single-series line chart for a lab parameter's trend over time — 2px line, flag-colored points, native tooltips, and a data table fallback for full accessibility. */
export function ResultsTrendChart({ series }: { series: ResultsTrendSeries }) {
  const { points } = series
  if (points.length === 0) {
    return <p className="py-6 text-center text-sm text-slate-400">No approved results yet for this test.</p>
  }

  const values = points.map((p) => p.value)
  const minValue = Math.min(...values)
  const maxValue = Math.max(...values)
  const valueRange = maxValue - minValue || 1
  const yPad = valueRange * 0.15

  const plotWidth = WIDTH - PAD.left - PAD.right
  const plotHeight = HEIGHT - PAD.top - PAD.bottom
  const x = (i: number) => PAD.left + (points.length === 1 ? plotWidth / 2 : (i / (points.length - 1)) * plotWidth)
  const y = (v: number) => PAD.top + plotHeight - ((v - (minValue - yPad)) / (valueRange + yPad * 2)) * plotHeight

  const linePath = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${x(i).toFixed(1)} ${y(p.value).toFixed(1)}`).join(' ')
  const gridLines = [0, 0.5, 1].map((t) => PAD.top + plotHeight * t)

  const hasAbnormal = points.some((p) => p.flag !== 'Normal')

  return (
    <div>
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} className="w-full" role="img" aria-label={`${series.parameterName} trend`}>
        {gridLines.map((gy) => (
          <line key={gy} x1={PAD.left} y1={gy} x2={WIDTH - PAD.right} y2={gy} stroke="#e2e8f0" strokeWidth={1} />
        ))}
        <path d={linePath} fill="none" stroke={SERIES_COLOR} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
        {points.map((p, i) => (
          <circle key={`${p.date}-${i}`} cx={x(i)} cy={y(p.value)} r={4} fill={FLAG_COLOR[p.flag] ?? SERIES_COLOR} stroke="#fcfcfb" strokeWidth={1.5}>
            <title>
              {p.date}: {p.value} {series.unit ?? ''} {p.flag !== 'Normal' ? `(${p.flag})` : ''}
            </title>
          </circle>
        ))}
      </svg>
      <div className="mt-1 flex items-center justify-between text-xs text-slate-400">
        <span>{points[0].date}</span>
        {hasAbnormal && (
          <span className="flex items-center gap-1">
            <span className="inline-block h-2 w-2 rounded-full" style={{ backgroundColor: FLAG_COLOR.Critical }} />
            Abnormal / critical value
          </span>
        )}
        <span>{points[points.length - 1].date}</span>
      </div>

      <details className="mt-2">
        <summary className="cursor-pointer text-xs text-primary-600 hover:underline">View as table</summary>
        <table className="mt-2 w-full text-xs">
          <thead>
            <tr className="border-b border-slate-200 text-left text-slate-500">
              <th className="py-1 pr-2">Date</th>
              <th className="py-1 pr-2">Value</th>
              <th className="py-1">Flag</th>
            </tr>
          </thead>
          <tbody>
            {points.map((p, i) => (
              <tr key={`${p.date}-${i}`} className="border-b border-slate-100 last:border-0">
                <td className="py-1 pr-2 text-slate-600">{p.date}</td>
                <td className="py-1 pr-2 text-slate-800">
                  {p.value} {series.unit ?? ''}
                </td>
                <td className="py-1" style={{ color: p.flag === 'Normal' ? undefined : FLAG_COLOR[p.flag] }}>
                  {p.flag}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </div>
  )
}
