import { QueryClientProvider } from '@tanstack/react-query'
import { ToastProvider } from './components/ui/Toast'
import { AuthProvider } from './features/auth/AuthContext'
import { queryClient } from './lib/queryClient'
import { ThemeProvider } from './lib/ThemeContext'
import { RootSwitch } from './routes'

function App() {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <AuthProvider>
            <RootSwitch />
          </AuthProvider>
        </ToastProvider>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App
