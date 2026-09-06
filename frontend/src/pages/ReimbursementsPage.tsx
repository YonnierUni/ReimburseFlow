import { useState } from 'react'
import { apiDocsUrl } from '../api/reimbursementsApi'
import { ReimbursementForm } from '../components/ReimbursementForm'
import { ReimbursementTable } from '../components/ReimbursementTable'
import { useReimbursements } from '../hooks/useReimbursements'
import { expenseCategories, reimbursementStatuses, type ExpenseCategory, type ReimbursementStatus } from '../models/reimbursement'

export function ReimbursementsPage() {
  const [status, setStatus] = useState<ReimbursementStatus | ''>('')
  const [category, setCategory] = useState<ExpenseCategory | ''>('')
  const [actionError, setActionError] = useState('')
  const filters = { status: status || undefined, category: category || undefined }
  const { items, loading, error, refresh } = useReimbursements(filters)

  return (
    <main className="app-shell">
      <header className="page-header">
        <div>
          <div className="eyebrow">Operations console / ReimburseFlow</div>
          <h1>Reimbursement Management</h1>
          <p>Review, approve, and track employee expense requests in one place.</p>
        </div>
        <a className="swagger-link" href={apiDocsUrl} target="_blank" rel="noreferrer">Open API docs <span aria-hidden="true">↗</span></a>
      </header>

      <div className="workspace-grid">
        <ReimbursementForm onCreated={refresh} />
        <section className="list-panel" aria-labelledby="requests-heading">
          <div className="list-heading">
            <div>
              <div className="section-kicker">Request queue</div>
              <h2 id="requests-heading">Expense requests</h2>
            </div>
            <div className="filter-bar" aria-label="Filter requests">
              <label> Status
                <select value={status} onChange={(event) => { setActionError(''); setStatus(event.target.value as ReimbursementStatus | '') }}>
                  <option value="">All</option>
                  {reimbursementStatuses.map((value) => <option key={value}>{value}</option>)}
                </select>
              </label>
              <label> Category
                <select value={category} onChange={(event) => { setActionError(''); setCategory(event.target.value as ExpenseCategory | '') }}>
                  <option value="">All</option>
                  {expenseCategories.map((value) => <option key={value}>{value}</option>)}
                </select>
              </label>
            </div>
          </div>

          {(error || actionError) && <div className="feedback error page-feedback" role="alert">{error || actionError}</div>}
          {loading && <div className="state-panel">Loading reimbursements...</div>}
          {!loading && !error && items.length === 0 && <div className="state-panel empty-state"><strong>No reimbursements found.</strong><span>New requests will appear here after they are created.</span></div>}
          {!loading && !error && items.length > 0 && <ReimbursementTable items={items} onChanged={refresh} onActionStart={() => setActionError('')} onActionSuccess={() => setActionError('')} onError={setActionError} />}
        </section>
      </div>
    </main>
  )
}
