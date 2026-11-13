export const inr = (n: number) => `₹${Math.round(n).toLocaleString('en-IN')}`

/** ₹3.1L / ₹2.4Cr / ₹45.2k — compact Indian notation for chart centres and axes. */
export function inrCompact(n: number): string {
  const abs = Math.abs(n)
  if (abs >= 1e7) return `₹${(n / 1e7).toFixed(1)}Cr`
  if (abs >= 1e5) return `₹${(n / 1e5).toFixed(1)}L`
  if (abs >= 1e3) return `₹${(n / 1e3).toFixed(1)}k`
  return `₹${Math.round(n)}`
}

export const DONUT_COLORS = ['#2348ff', '#2dd4bf', '#7a5bff', '#c6a15b', '#ff9f6b', '#8a94ad', '#0e9d8b', '#ff5470']
