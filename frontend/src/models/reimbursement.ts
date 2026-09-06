export const expenseCategories = [
  'Transportation',
  'Food',
  'Accommodation',
  'Other',
] as const

export const reimbursementStatuses = ['Pending', 'Approved', 'Rejected'] as const

export type ExpenseCategory = (typeof expenseCategories)[number]
export type ReimbursementStatus = (typeof reimbursementStatuses)[number]

export type Reimbursement = {
  id: string
  employeeId: string
  receiptNumber: string
  expenseDate: string
  category: ExpenseCategory
  description: string
  amount: number
  status: ReimbursementStatus
  rejectionReason: string | null
  createdAt: string
  updatedAt: string | null
}

export type CreateReimbursementRequest = {
  employeeId: string
  receiptNumber: string
  expenseDate: string
  category: ExpenseCategory
  description: string
  amount: number
}

export type ProblemDetails = {
  status?: number
  title?: string
  detail?: string
  traceId?: string
}

export type ReimbursementFilters = {
  status?: ReimbursementStatus
  category?: ExpenseCategory
}
