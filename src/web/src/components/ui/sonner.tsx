import { Toaster as SonnerToaster } from 'sonner'

// Thin wrapper over sonner's <Toaster />. Hand-written rather than pulled from
// the shadcn registry, which couples it to next-themes; this app has no theme
// provider, so we let sonner follow prefers-color-scheme itself.
export function Toaster() {
  return (
    <SonnerToaster
      theme="system"
      position="top-right"
      richColors
      closeButton
    />
  )
}