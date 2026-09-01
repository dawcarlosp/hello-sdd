import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { uploadTemplate } from '../api'

export default function UploadPage() {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [uploadedId, setUploadedId] = useState<number | null>(null)
  const [warnings, setWarnings] = useState<string[]>([])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    if (name.trim().length === 0) {
      setError('El nombre de la plantilla es obligatorio.')
      return
    }
    if (file === null) {
      setError('Selecciona un archivo .docx.')
      return
    }
    setSubmitting(true)
    try {
      const result = await uploadTemplate(file, name.trim())
      setUploadedId(result.templateId)
      setWarnings(result.warnings)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error inesperado.')
    } finally {
      setSubmitting(false)
    }
  }

  if (uploadedId !== null) {
    return (
      <div>
        <h2>Plantilla subida correctamente</h2>
        {warnings.length > 0 && (
          <ul className="warning">
            {warnings.map(warning => <li key={warning}>{warning}</li>)}
          </ul>
        )}
        <p>
          <button className="button" onClick={() => navigate(`/plantillas/${uploadedId}`)}>
            Rellenar ahora
          </button>{' '}
          <Link to="/">Volver a la lista</Link>
        </p>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit}>
      <h2>Subir plantilla</h2>
      <p className="hint">
        El documento debe ser un .docx con variables escritas como{' '}
        <code>$$variable$$</code>; dentro de una tabla, las variables de la
        misma fila se rellenan por filas.
      </p>
      <label>
        Nombre de la plantilla
        <input
          type="text"
          value={name}
          maxLength={200}
          onChange={event => setName(event.target.value)}
        />
      </label>
      <label>
        Archivo .docx
        <input
          type="file"
          accept=".docx"
          onChange={event => setFile(event.target.files?.[0] ?? null)}
        />
      </label>
      {error !== null && <p className="error">{error}</p>}
      <button className="button" type="submit" disabled={submitting}>
        {submitting ? 'Subiendo…' : 'Subir plantilla'}
      </button>
    </form>
  )
}
