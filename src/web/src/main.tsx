import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { ErrorBoundary } from '@/components/ErrorBoundary'

const root = document.getElementById('root')!

createRoot(root).render(
  <StrictMode>
    <ErrorBoundary>
      <App />
    </ErrorBoundary>
  </StrictMode>,
)

// React clears #root on mount; drop the pre-React boot fallback explicitly too,
// in case that behaviour ever changes.
document.getElementById('boot-fallback')?.remove()