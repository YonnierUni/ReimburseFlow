import { useState } from 'react'
import { createReimbursement } from '../api/reimbursementsApi'
import {
  expenseCategories,
  type CreateReimbursementRequest,
} from '../models/reimbursement'

type ReimbursementFormProps = { onCreated: () => Promise<void> | void }

type FormState = CreateReimbursementRequest

const initialForm: FormState = {
  employeeId: '',
  receiptNumber: '',
  expenseDate: '',
  category: 'Transportation',
  description: '',
  amount: 0,
}

export function ReimbursementForm({ onCreated }: ReimbursementFormProps) {
  const [form, setForm] = useState(initialForm)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [attemptKey, setAttemptKey] = useState<string | null>(null)

  const update = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setAttemptKey(null)
    setError('')
    setSuccess('')
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (form.amount <= 0) {
      setError('Amount must be greater than zero.')
      return
    }

    const key = attemptKey ?? crypto.randomUUID()
    setAttemptKey(key)
    setSubmitting(true)
    setError('')
    setSuccess('')
    try {
      await createReimbursement(form, key)
      setForm(initialForm)
      setAttemptKey(null)
      setSuccess('Reimbursement created successfully.')
      await onCreated()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Could not connect to the server.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="form-panel" aria-labelledby="create-heading">
      <div className="section-kicker">New request</div>
      <h2 id="create-heading">Create reimbursement</h2>
      <p className="muted">Capture the expense details and send it for review.</p>
      <form className="reimbursement-form" onSubmit={handleSubmit}>
        <label>
          Employee ID
          <input required value={form.employeeId} onChange={(event) => update('employeeId', event.target.value)} />
        </label>
        <label>
          Receipt Number
          <input required value={form.receiptNumber} onChange={(event) => update('receiptNumber', event.target.value)} />
        </label>
        <label>
          Expense Date
          <input required type="date" value={form.expenseDate} onChange={(event) => update('expenseDate', event.target.value)} />
        </label>
        <label>
          Category
          <select value={form.category} onChange={(event) => update('category', event.target.value as FormState['category'])}>
            {expenseCategories.map((category) => <option key={category}>{category}</option>)}
          </select>
        </label>
        <label className="field-wide">
          Description
          <textarea required rows={3} value={form.description} onChange={(event) => update('description', event.target.value)} />
        </label>
        <label>
          Amount
          <input required min="0.01" step="0.01" type="number" value={form.amount || ''} onChange={(event) => update('amount', Number(event.target.value))} />
        </label>
        <div className="form-actions field-wide">
          <button className="primary-button" disabled={submitting} type="submit">
            {submitting ? 'Creating...' : 'Create reimbursement'}
          </button>
        </div>
      </form>
      {error && <p className="feedback error" role="alert">{error}</p>}
      {success && <p className="feedback success" role="status">{success}</p>}
    </section>
  )
}
