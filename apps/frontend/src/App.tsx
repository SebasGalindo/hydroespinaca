import './App.css'

// Update the import path below to the correct relative path where useAuth is located
import { useAuth } from "@shared-ui/hooks/useAuth"


export function App() {
  const { user } = useAuth()
  return <h1>Hello {user.name}</h1>
}

export default App
