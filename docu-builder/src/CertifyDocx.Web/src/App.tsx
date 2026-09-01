import { NavLink, Route, Routes } from 'react-router-dom'
import TemplatesPage from './pages/TemplatesPage'
import UploadPage from './pages/UploadPage'
import FillPage from './pages/FillPage'

export default function App() {
  return (
    <div className="app">
      <header className="app-header">
        <h1>CertifyDocx</h1>
        <nav>
          <NavLink to="/">Plantillas</NavLink>
          <NavLink to="/subir">Subir plantilla</NavLink>
        </nav>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<TemplatesPage />} />
          <Route path="/subir" element={<UploadPage />} />
          <Route path="/plantillas/:id" element={<FillPage />} />
        </Routes>
      </main>
    </div>
  )
}
