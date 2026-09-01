import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listTemplates } from '../api'
import type { TemplateSummary } from '../types'

export default function TemplatesPage() {
  const [templates, setTemplates] = useState<TemplateSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    listTemplates()
      .then(setTemplates)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Error inesperado.'))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <p>Cargando plantillas…</p>
  if (error) return <p className="error">{error}</p>

  if (templates.length === 0) {
    return (
      <div>
        <p>Todavía no hay plantillas guardadas.</p>
        <Link className="button" to="/subir">Subir la primera plantilla</Link>
      </div>
    )
  }

  return (
    <div>
      <h2>Plantillas guardadas</h2>
      <table className="templates-table">
        <thead>
          <tr>
            <th>Nombre</th>
            <th>Variables</th>
            <th>Subida</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {templates.map(template => (
            <tr key={template.templateId}>
              <td>{template.name}</td>
              <td>{template.variableCount}</td>
              <td>{new Date(template.createdAt).toLocaleString('es-ES')}</td>
              <td>
                <Link className="button" to={`/plantillas/${template.templateId}`}>Rellenar</Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
