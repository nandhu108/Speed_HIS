// Alpha values at/above this are treated as "confidently foreground" and forced fully opaque;
// everything else is forced fully transparent. The model outputs a soft matte, so low-confidence
// regions (e.g. a second, out-of-focus person in the background) land at partial alpha and would
// otherwise alpha-blend into the white backdrop as a faint ghost instead of disappearing. A hard
// cutout loses a little edge anti-aliasing on the primary subject's hair, which is the right
// trade-off for an ID-style photo over leaving stray people half-visible.
const ALPHA_THRESHOLD = 160

function binarizeAlpha(bitmap: ImageBitmap): Promise<ImageBitmap> {
  const canvas = document.createElement('canvas')
  canvas.width = bitmap.width
  canvas.height = bitmap.height
  const ctx = canvas.getContext('2d')
  if (!ctx) throw new Error('Canvas 2D context unavailable')
  ctx.drawImage(bitmap, 0, 0)

  const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height)
  const data = imageData.data
  for (let i = 3; i < data.length; i += 4) {
    data[i] = data[i] < ALPHA_THRESHOLD ? 0 : 255
  }
  ctx.putImageData(imageData, 0, 0)

  return createImageBitmap(canvas)
}

/**
 * Runs fully client-side background segmentation (WASM, in a worker) and composites the cutout
 * onto a plain white background — a passport/ID-photo look. Only the one-time model download
 * touches the network; the patient image itself never leaves the browser. `isnet_fp16` is the
 * library's own default ("medium", ~80MB) — the smaller quantized `isnet_quint8` model trades
 * away too much mask quality (visible noise/artifacts around hair and edges) for the size saving.
 *
 * `@imgly/background-removal` drags in onnxruntime-web (a many-MB WASM engine) — this app has no
 * route-level code-splitting, so a static import would ship that to every page for every user.
 * The dynamic import keeps it out of the main bundle entirely; it's only fetched the first time
 * someone actually turns the "auto-remove background" toggle on.
 */
export async function removeBackgroundOnWhite(input: Blob): Promise<Blob> {
  const { removeBackground } = await import('@imgly/background-removal')
  const cutout = await removeBackground(input, {
    model: 'isnet_fp16',
    output: { format: 'image/png' },
  })

  const rawBitmap = await createImageBitmap(cutout)
  const bitmap = await binarizeAlpha(rawBitmap)
  rawBitmap.close()

  const canvas = document.createElement('canvas')
  canvas.width = bitmap.width
  canvas.height = bitmap.height
  const ctx = canvas.getContext('2d')
  if (!ctx) throw new Error('Canvas 2D context unavailable')
  ctx.fillStyle = '#ffffff'
  ctx.fillRect(0, 0, canvas.width, canvas.height)
  ctx.drawImage(bitmap, 0, 0)
  bitmap.close()

  return await new Promise<Blob>((resolve, reject) => {
    canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('Failed to encode processed image'))), 'image/jpeg', 0.92)
  })
}
