// קומפוננטת שורש — SmartHome
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { STRINGS } from './utils/strings'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<div style={{ padding: '2rem', textAlign: 'center' }}>
          <h1 style={{ color: '#1E3A5F' }}>{STRINGS.app.name}</h1>
          <p>{STRINGS.app.tagline}</p>
          <p style={{ marginTop: '1rem', color: '#27AE60' }}>✅ הפרויקט עולה בהצלחה!</p>
        </div>} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
