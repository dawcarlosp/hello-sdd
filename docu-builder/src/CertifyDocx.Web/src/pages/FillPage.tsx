import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { generateDocument, getTemplate } from '../api'
import type { TemplateDetail } from '../types'

const MAX_ROWS = 100
const MAX_FIELD_LENGTH = 1000

type RowsByGroup = Record<number, Record<string, string>[]>

function emptyRow(variables: string[]): Record<string, string> {
  const row: Record<string, string> = {}
  for (const name of variables) row[name] = ''
  return row
}

export default function FillPage() {
  const { id } = useParams()
  const [template, setTemplate] = useState<TemplateDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [simpleValues, setSimpleValues] = useState<Record<string, string>>({})
  const [rowsByGroup, setRowsByGroup] = useState<RowsByGroup>({})
  const [missing, setMissing] = useState<string[]>([])
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [downloaded, setDownloaded] = useState(false)

  useEffect(() => {
    getTemplate(Number(id))
      .then(detail => {
        setTemplate(detail)
        const simples: Record<string, string> = {}
        for (const variable of detail.schema.variables) {
          if (variable.kind === 'simple') simples[variable.name] = ''
        }
        setSimpleValues(simples)
        const rows: RowsByGroup = {}
        for (const group of detail.schema.rowGroups) {
          rows[group.rowGroupId] = [emptyRow(group.variables)]
        }
        setRowsByGroup(rows)
      })
      .catch((err: unknown) => setLoadError(err instanceof Error ? err.message : 'Error inesperado.'))
      .finally(() => setLoading(false))
  }, [id])

  const simpleVariables = useMemo(
    () => (template?.schema.variables ?? []).filter(variable => variable.kind === 'simple'),
    [template],
  )

  if (loading) return <p>Cargando formulario…</p>
  if (loadError) return <p className="error">{loadError}</p>
  if (template === null) return <p className="error">Plantilla no encontrada.</p>

  function validate(): string[] {
    const missingFields: string[] = []
    if (template === null) return missingFields
    for (const variable of simpleVariables) {
      if ((simpleValues[variable.name] ?? '').trim().length === 0) {
        missingFields.push(variable.name)
      }
    }
    for (const group of template.schema.rowGroups) {
      const rows = rowsByGroup[group.rowGroupId] ?? []
      rows.forEach((row, index) => {
        for (const variableName of group.variables) {
          if ((row[variableName] ?? '').trim().length === 0) {
            missingFields.push(`fila ${index + 1}: ${variableName}`)
          }
        }
      })
    }
    return missingFields
  }

  function addRow(rowGroupId: number, variables: string[]) {
    setRowsByGroup(current => ({
      ...current,
      [rowGroupId]: [...(current[rowGroupId] ?? []), emptyRow(variables)],
    }))
    setDownloaded(false)
  }

  function removeRow(rowGroupId: number, index: number) {
    setRowsByGroup(current => ({
      ...current,
      [rowGroupId]: (current[rowGroupId] ?? []).filter((_, i) => i !== index),
    }))
    setDownloaded(false)
  }

  function updateCell(rowGroupId: number, index: number, variableName: string, value: string) {
    setRowsByGroup(current => {
      const rows = [...(current[rowGroupId] ?? [])]
      rows[index] = { ...rows[index], [variableName]: value }
      return { ...current, [rowGroupId]: rows }
    })
    setDownloaded(false)
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitError(null)
    setDownloaded(false)
    const missingFields = validate()
    setMissing(missingFields)
    if (missingFields.length > 0 || template === null) return

    setSubmitting(true)
    try {
      const blob = await generateDocument(template.templateId, {
        simpleValues,
        rowValues: template.schema.rowGroups.map(group => ({
          rowGroupId: group.rowGroupId,
          rows: rowsByGroup[group.rowGroupId] ?? [],
        })),
      })
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = `${template.name}.docx`
      anchor.click()
      URL.revokeObjectURL(url)
      setDownloaded(true)
    } catch (err: unknown) {
      setSubmitError(err instanceof Error ? err.message : 'Error inesperado.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <h2>Rellenar: {template.name}</h2>

      {simpleVariables.length > 0 && (
        <fieldset>
          <legend>Datos</legend>
          {simpleVariables.map(variable => (
            <label key={variable.name}>
              {variable.name}
              <input
                type="text"
                value={simpleValues[variable.name] ?? ''}
                maxLength={MAX_FIELD_LENGTH}
                onChange={event => {
                  setSimpleValues(current => ({ ...current, [variable.name]: event.target.value }))
                  setDownloaded(false)
                }}
              />
            </label>
          ))}
        </fieldset>
      )}

      {template.schema.rowGroups.map(group => {
        const rows = rowsByGroup[group.rowGroupId] ?? []
        return (
          <fieldset key={group.rowGroupId}>
            <legend>
              Filas (tabla {group.tableIndex + 1}, fila {group.rowIndex + 1})
            </legend>
            <table className="rows-table">
              <thead>
                <tr>
                  {group.variables.map(variableName => <th key={variableName}>{variableName}</th>)}
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row, index) => (
                  <tr key={index}>
                    {group.variables.map(variableName => (
                      <td key={variableName}>
                        <input
                          type="text"
                          value={row[variableName] ?? ''}
                          maxLength={MAX_FIELD_LENGTH}
                          onChange={event => updateCell(group.rowGroupId, index, variableName, event.target.value)}
                        />
                      </td>
                    ))}
                    <td>
                      <button type="button" onClick={() => removeRow(group.rowGroupId, index)}>
                        Quitar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {rows.length === 0 && (
              <p className="hint">Sin filas: la fila de la plantilla se eliminará del documento.</p>
            )}
            {rows.length < MAX_ROWS ? (
              <button type="button" onClick={() => addRow(group.rowGroupId, group.variables)}>
                Añadir fila
              </button>
            ) : (
              <p className="warning">Límite de {MAX_ROWS} filas alcanzado.</p>
            )}
          </fieldset>
        )
      })}

      {missing.length > 0 && (
        <p className="error">Faltan campos obligatorios: {missing.join(', ')}.</p>
      )}
      {submitError !== null && <p className="error">{submitError}</p>}
      {downloaded && <p className="success">Documento descargado.</p>}

      <button className="button" type="submit" disabled={submitting}>
        {submitting ? 'Generando…' : 'Generar y descargar'}
      </button>
    </form>
  )
}
