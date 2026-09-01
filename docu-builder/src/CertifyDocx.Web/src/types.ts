export interface VariableInfo {
  name: string
  kind: 'simple' | 'row'
  rowGroupId: number
}

export interface RowGroupInfo {
  rowGroupId: number
  tableIndex: number
  rowIndex: number
  variables: string[]
}

export interface TemplateSchema {
  variables: VariableInfo[]
  rowGroups: RowGroupInfo[]
}

export interface TemplateSummary {
  templateId: number
  name: string
  variableCount: number
  createdAt: string
}

export interface TemplateDetail {
  templateId: number
  name: string
  schema: TemplateSchema
}

export interface UploadResult {
  templateId: number
  name: string
  schema: TemplateSchema
  warnings: string[]
}
