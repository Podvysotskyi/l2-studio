import StudioConfirmDialog from '../components/app/StudioConfirmDialog.vue'
import StudioPromptDialog from '../components/app/StudioPromptDialog.vue'

type DialogColor = 'primary' | 'neutral' | 'error' | 'warning' | 'success' | 'info'
export type StudioConfirmResult = boolean | 'all'

export interface StudioConfirmOptions {
  title: string
  description?: string
  confirmLabel?: string
  cancelLabel?: string
  confirmColor?: DialogColor
  alternativeLabel?: string
}

export interface StudioPromptOptions {
  title: string
  description?: string
  label: string
  initialValue?: string
  confirmLabel?: string
  cancelLabel?: string
}

export function useStudioDialogs() {
  const overlay = useOverlay()
  const confirmDialog = overlay.create(StudioConfirmDialog)
  const promptDialog = overlay.create(StudioPromptDialog)
  let queue: Promise<void> = Promise.resolve()

  function enqueue<T>(open: () => Promise<T>): Promise<T> {
    const result = queue.then(open, open)
    queue = result.then(() => undefined, () => undefined)
    return result
  }

  function confirm(options: StudioConfirmOptions): Promise<StudioConfirmResult> {
    return enqueue(async () => {
      const result = await confirmDialog.open(options)
      return result === 'all' ? 'all' : Boolean(result)
    })
  }

  function prompt(options: StudioPromptOptions): Promise<string | undefined> {
    return enqueue(async () => await promptDialog.open(options))
  }

  return { confirm, prompt }
}
