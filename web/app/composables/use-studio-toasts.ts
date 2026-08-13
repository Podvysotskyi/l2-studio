interface StudioToastOptions {
  title: string
  description?: string
}

export function useStudioToasts() {
  const toast = useToast()

  function success(options: StudioToastOptions) {
    toast.add({
      ...options,
      color: 'success',
      icon: 'i-lucide-circle-check',
      duration: 5000
    })
  }

  function warning(options: StudioToastOptions) {
    toast.add({
      ...options,
      color: 'warning',
      icon: 'i-lucide-triangle-alert',
      duration: 7000
    })
  }

  function error(options: StudioToastOptions) {
    toast.add({
      ...options,
      color: 'error',
      icon: 'i-lucide-circle-alert',
      duration: 7000
    })
  }

  return { success, warning, error }
}
