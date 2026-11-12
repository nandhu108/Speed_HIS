import { Camera, Check, RotateCcw, Sparkles, Upload, Video, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { api, errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Select } from '../../../components/ui/Select'
import { removeBackgroundOnWhite } from '../../../lib/backgroundRemoval'
import { useUploadPatientPhoto } from '../api'

type Phase = 'idle' | 'starting' | 'live' | 'review'

interface PatientPhotoCaptureProps {
  /** Existing patient — captures upload straight to `/patients/{id}/photo`. `null` for a patient
   *  still being registered: the widget captures locally and hands the file back via `onCaptured`
   *  instead, so the caller can upload it right after the patient record is created. */
  patientId: number | null
  /** Used for the captured file's name and image alt text. */
  personLabel: string
  /** Deferred mode only — fires with the confirmed file once the user hits "Save"/picks a file. */
  onCaptured?: (file: File) => void
  /** Fires after a successful immediate upload (patientId mode). */
  onUploaded?: () => void
  /** Smaller preview for embedding inside modals/wizard steps instead of a standalone page. */
  compact?: boolean
  /** Remounts the capture state (e.g. when the selected patient changes). Defaults to patientId. */
  resetKey?: string | number | null
}

/**
 * Shared camera-capture widget behind Photo Capture, the patient-detail Photo card, and the
 * admit/registration flows — live preview with a device picker for multi-camera machines, a
 * capture → review → save step so a bad shot never overwrites the photo on file, and a file-upload
 * fallback throughout.
 */
export function PatientPhotoCapture({
  patientId, personLabel, onCaptured, onUploaded, compact, resetKey = patientId,
}: PatientPhotoCaptureProps) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [phase, setPhase] = useState<Phase>('idle')
  const [devices, setDevices] = useState<MediaDeviceInfo[]>([])
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null)
  const [capturedUrl, setCapturedUrl] = useState<string | null>(null)
  const [capturedBlob, setCapturedBlob] = useState<Blob | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [removeBg, setRemoveBg] = useState(false)
  const [processing, setProcessing] = useState(false)

  const videoRef = useRef<HTMLVideoElement>(null)
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  // The pristine, never-processed shot behind the current review preview — kept so toggling
  // "auto-remove background" while a photo is already under review can re-run (or undo)
  // processing against the original instead of doing nothing until the next Retake.
  const rawBlobRef = useRef<Blob | null>(null)

  const upload = useUploadPatientPhoto()

  const stopTracks = () => {
    streamRef.current?.getTracks().forEach((track) => track.stop())
    streamRef.current = null
  }

  const cancelCamera = () => {
    stopTracks()
    setPhase('idle')
  }

  const discardReview = () => {
    if (capturedUrl) URL.revokeObjectURL(capturedUrl)
    setCapturedUrl(null)
    setCapturedBlob(null)
    rawBlobRef.current = null
    setPhase('idle')
  }

  useEffect(() => stopTracks, [])

  // Reload the existing photo (immediate mode) / reset to blank (deferred mode) whenever the
  // caller signals a new subject — a different selected patient, or a fresh registration attempt.
  useEffect(() => {
    stopTracks()
    setPhase('idle')
    setError(null)
    setSuccess(false)
    if (capturedUrl) URL.revokeObjectURL(capturedUrl)
    setCapturedUrl(null)
    setCapturedBlob(null)
    rawBlobRef.current = null
    let objectUrl: string | null = null
    let cancelled = false
    setPreviewUrl(null)
    if (!patientId) return

    api
      .get(`/patients/${patientId}/photo`, { responseType: 'blob' })
      .then(({ data }) => {
        if (cancelled) return
        objectUrl = URL.createObjectURL(data as Blob)
        setPreviewUrl(objectUrl)
      })
      .catch(() => {
        // No photo on file yet — leave the preview empty, not an error.
      })

    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resetKey])

  const refreshDevices = async (activeStream: MediaStream) => {
    try {
      const list = await navigator.mediaDevices.enumerateDevices()
      const cameras = list.filter((d) => d.kind === 'videoinput')
      setDevices(cameras)
      const activeId = activeStream.getVideoTracks()[0]?.getSettings().deviceId
      setSelectedDeviceId(activeId ?? cameras[0]?.deviceId ?? null)
    } catch {
      // enumerateDevices can fail without a secure context in some browsers — fall back to a
      // single implicit camera with no picker, rather than blocking capture entirely.
    }
  }

  const startCamera = async (deviceId?: string) => {
    setError(null)
    setPhase('starting')
    stopTracks()
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: deviceId ? { deviceId: { exact: deviceId } } : { facingMode: 'user' },
      })
      streamRef.current = stream
      if (videoRef.current) {
        videoRef.current.srcObject = stream
        await videoRef.current.play()
      }
      setPhase('live')
      await refreshDevices(stream)
    } catch {
      setPhase('idle')
      setError('Could not access the camera — check browser permissions, or upload a photo file instead.')
    }
  }

  const handleDeviceChange = (deviceId: string) => {
    setSelectedDeviceId(deviceId)
    startCamera(deviceId)
  }

  // Runs auto background removal against a pristine source blob when `want` is true; on failure
  // (e.g. offline — the model downloads from a CDN on first use) falls back to the original
  // instead of blocking. Takes `want` as a parameter rather than reading the `removeBg` state so
  // the checkbox's own change handler can call this with the *new* value before the re-render
  // that would otherwise make it visible via the closure.
  const applyProcessing = async (raw: Blob, want: boolean): Promise<Blob> => {
    if (!want) return raw
    setProcessing(true)
    try {
      return await removeBackgroundOnWhite(raw)
    } catch {
      setError('Background removal unavailable right now — kept the original shot.')
      return raw
    } finally {
      setProcessing(false)
    }
  }

  const capture = () => {
    if (!videoRef.current || !canvasRef.current) return
    const video = videoRef.current
    const canvas = canvasRef.current
    canvas.width = video.videoWidth
    canvas.height = video.videoHeight
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    ctx.drawImage(video, 0, 0, canvas.width, canvas.height)

    canvas.toBlob(
      async (blob) => {
        if (!blob) return
        stopTracks()
        setError(null)
        rawBlobRef.current = blob
        // Show the raw shot immediately, then swap in the processed version once ready — the
        // background-removal pass takes a beat and the user shouldn't stare at a blank preview.
        setCapturedBlob(blob)
        setCapturedUrl(URL.createObjectURL(blob))
        setPhase('review')
        const processed = await applyProcessing(blob, removeBg)
        if (processed !== blob) {
          setCapturedBlob(processed)
          setCapturedUrl((prev) => {
            if (prev) URL.revokeObjectURL(prev)
            return URL.createObjectURL(processed)
          })
        }
      },
      'image/jpeg',
      0.92,
    )
  }

  const retake = () => {
    if (capturedUrl) URL.revokeObjectURL(capturedUrl)
    setCapturedUrl(null)
    setCapturedBlob(null)
    rawBlobRef.current = null
    startCamera(selectedDeviceId ?? undefined)
  }

  // Re-runs (or undoes) background removal against the untouched shot still under review — so
  // flipping the checkbox after the fact takes effect immediately instead of silently doing
  // nothing until the next Retake, which is the "checked the box but nothing changed" bug.
  const handleRemoveBgToggle = async (checked: boolean) => {
    setRemoveBg(checked)
    setError(null)
    const raw = rawBlobRef.current
    if (phase !== 'review' || !raw) return
    const result = await applyProcessing(raw, checked)
    setCapturedBlob(result)
    setCapturedUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev)
      return URL.createObjectURL(result)
    })
  }

  const commit = async (file: File, resultUrl: string) => {
    setError(null)
    try {
      if (patientId) {
        await upload.mutateAsync({ patientId, file })
        onUploaded?.()
      } else {
        onCaptured?.(file)
      }
      if (previewUrl) URL.revokeObjectURL(previewUrl)
      setPreviewUrl(resultUrl)
      setSuccess(true)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const savePhoto = async () => {
    if (!capturedBlob || !capturedUrl || processing) return
    const file = new File([capturedBlob], `${personLabel || 'patient'}.jpg`, { type: 'image/jpeg' })
    await commit(file, capturedUrl)
    setCapturedUrl(null)
    setCapturedBlob(null)
    setPhase('idle')
  }

  const onFilePicked = async (file: File | undefined) => {
    if (!file) return
    setSuccess(false)
    setError(null)
    const processed = await applyProcessing(file, removeBg)
    const outFile =
      processed === file ? file : new File([processed], `${personLabel || 'patient'}.jpg`, { type: 'image/jpeg' })
    await commit(outFile, URL.createObjectURL(processed))
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const previewSize = compact ? 'w-64' : 'w-full sm:w-72'

  return (
    <div className="flex flex-col gap-5 sm:flex-row sm:items-start">
      <div className={`shrink-0 space-y-2 ${previewSize}`}>
        <div className="relative aspect-[4/3] w-full overflow-hidden rounded-shell-sm border border-border-soft bg-panel-2">
          {phase === 'live' || phase === 'starting' ? (
            // eslint-disable-next-line jsx-a11y/media-has-caption
            <video ref={videoRef} className="h-full w-full -scale-x-100 object-cover" muted playsInline />
          ) : phase === 'review' && capturedUrl ? (
            <img src={capturedUrl} alt="Captured, not yet saved" className="h-full w-full object-cover" />
          ) : previewUrl ? (
            <img src={previewUrl} alt={personLabel} className="h-full w-full object-cover" />
          ) : (
            <div className="flex h-full w-full flex-col items-center justify-center gap-2 text-ink-faint">
              <Camera className={compact ? 'size-6' : 'size-8'} aria-hidden />
              <span className="text-xs">No photo</span>
            </div>
          )}

          {phase === 'starting' && (
            <div className="absolute inset-0 flex items-center justify-center gap-2 bg-panel-2/90 text-xs text-ink-muted">
              <span className="size-3.5 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden />
              Starting…
            </div>
          )}
          {phase === 'review' && (
            <span className="absolute top-2 left-2 rounded-full bg-ink/70 px-2 py-0.5 text-[10px] font-semibold text-white">
              Not saved yet
            </span>
          )}
          {processing && (
            <div className="absolute inset-x-0 bottom-0 flex items-center justify-center gap-2 bg-ink/70 px-2 py-1.5 text-[11px] text-white">
              <span className="size-3 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden />
              Removing background…
            </div>
          )}
        </div>
        {phase === 'live' && !compact && (
          <p className="text-[11px] text-ink-faint">Preview is mirrored for framing — the saved photo will be right-reading.</p>
        )}
      </div>

      <div className="flex min-w-0 flex-1 flex-col gap-2">
        {error && <p className="rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
        {success && (
          <p className="rounded-shell-sm bg-ok/10 px-3 py-2 text-sm text-ok">
            {patientId ? 'Photo saved.' : 'Photo ready — it will be saved with the registration.'}
          </p>
        )}

        {(phase === 'live' || phase === 'starting') && devices.length > 1 && (
          <Select
            label="Camera"
            value={selectedDeviceId ?? ''}
            onChange={(e) => handleDeviceChange(e.target.value)}
            options={devices.map((d, i) => ({ value: d.deviceId, label: d.label || `Camera ${i + 1}` }))}
          />
        )}

        <label className="flex items-center gap-2 text-xs text-ink-muted select-none">
          <input
            type="checkbox"
            checked={removeBg}
            disabled={phase === 'live' || phase === 'starting' || processing}
            onChange={(e) => handleRemoveBgToggle(e.target.checked)}
            className="size-3.5 rounded border-border-soft accent-accent-2"
          />
          <Sparkles className="size-3.5 text-ink-faint" aria-hidden />
          Auto-remove background (white backdrop)
        </label>

        {phase === 'idle' && (
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" onClick={() => startCamera(selectedDeviceId ?? undefined)}>
              <Video className="mr-1.5 size-4" aria-hidden />
              Start camera
            </Button>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="hidden"
              onChange={(e) => onFilePicked(e.target.files?.[0])}
            />
            <Button variant="ghost" loading={upload.isPending || processing} onClick={() => fileInputRef.current?.click()}>
              <Upload className="mr-1.5 size-4" aria-hidden />
              Upload from file
            </Button>
          </div>
        )}

        {phase === 'starting' && (
          <Button variant="ghost" disabled>
            Starting…
          </Button>
        )}

        {phase === 'live' && (
          <div className="flex flex-wrap gap-2">
            <Button onClick={capture}>
              <Camera className="mr-1.5 size-4" aria-hidden />
              Capture
            </Button>
            <Button variant="ghost" onClick={cancelCamera}>
              <X className="mr-1.5 size-4" aria-hidden />
              Cancel
            </Button>
          </div>
        )}

        {phase === 'review' && (
          <div className="flex flex-wrap gap-2">
            <Button onClick={savePhoto} loading={upload.isPending} disabled={processing}>
              <Check className="mr-1.5 size-4" aria-hidden />
              Save photo
            </Button>
            <Button variant="secondary" onClick={retake} disabled={processing}>
              <RotateCcw className="mr-1.5 size-4" aria-hidden />
              Retake
            </Button>
            <Button variant="ghost" onClick={discardReview}>
              Discard
            </Button>
          </div>
        )}
      </div>
      <canvas ref={canvasRef} className="hidden" />
    </div>
  )
}
