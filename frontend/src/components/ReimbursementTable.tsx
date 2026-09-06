import { useRef, useState } from 'react'
import { ApiError, approveReimbursement, rejectReimbursement } from '../api/reimbursementsApi'
import type { Reimbursement } from '../models/reimbursement'
import { RejectReimbursementModal } from './RejectReimbursementModal'
import { StatusBadge } from './StatusBadge'

type ReimbursementTableProps = {
  items: Reimbursement[]
  onChanged: () => Promise<void> | void
  onActionStart: () => void
  onActionSuccess: () => void
  onError: (message: string) => void
}

const amountFormatter = new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })

export function ReimbursementTable({ items, onChanged, onActionStart, onActionSuccess, onError }: ReimbursementTableProps) {
  const [activeId, setActiveId] = useState<string | null>(null)
  const [rejectingId, setRejectingId] = useState<string | null>(null)
  const activeRequestId = useRef<string | null>(null)
  const actionInProgress = activeId !== null

  function beginAction(id: string) {
    if (activeRequestId.current) return false
    activeRequestId.current = id
    setActiveId(id)
    onActionStart()
    return true
  }

  async function refreshAfterConflict(caught: unknown) {
    if (caught instanceof ApiError && caught.status === 409) {
      onError(caught.message)
      await onChanged()
      return true
    }

    return false
  }

  async function approve(id: string) {
    if (!beginAction(id)) return
    try {
      await approveReimbursement(id)
      await onChanged()
      onActionSuccess()
    } catch (caught) {
      if (!(await refreshAfterConflict(caught))) {
        onError(caught instanceof Error ? caught.message : 'Could not connect to the server.')
      }
    } finally {
      activeRequestId.current = null
      setActiveId(null)
    }
  }

  async function reject(reason: string) {
    if (!rejectingId) return
    if (!beginAction(rejectingId)) return
    try {
      await rejectReimbursement(rejectingId, reason)
      setRejectingId(null)
      await onChanged()
      onActionSuccess()
    } catch (caught) {
      if (await refreshAfterConflict(caught)) {
        setRejectingId(null)
      } else {
        onError(caught instanceof Error ? caught.message : 'Could not connect to the server.')
      }
    } finally {
      activeRequestId.current = null
      setActiveId(null)
    }
  }

  return (
    <>
      <div className="table-wrap">
        <table>
          <thead>
            <tr><th>Employee</th><th>Receipt</th><th>Date</th><th>Category</th><th>Description</th><th>Amount</th><th>Status</th><th>Actions</th></tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.id}>
                <td data-label="Employee">{item.employeeId}</td>
                <td data-label="Receipt">{item.receiptNumber}</td>
                <td data-label="Date">{item.expenseDate}</td>
                <td data-label="Category">{item.category}</td>
                <td data-label="Description" className="description-cell">{item.description}</td>
                <td data-label="Amount" className="amount-cell">{amountFormatter.format(item.amount)}</td>
                <td data-label="Status"><StatusBadge status={item.status} /></td>
                <td data-label="Actions">
                  {item.status === 'Pending' ? (
                    <div className="row-actions">
                      <button className="approve-button" disabled={actionInProgress} onClick={() => void approve(item.id)} type="button">Approve</button>
                      <button className="reject-button" disabled={actionInProgress} onClick={() => { if (!activeRequestId.current) { onActionStart(); setRejectingId(item.id) } }} type="button">Reject</button>
                    </div>
                  ) : <span className="muted">No actions</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <RejectReimbursementModal open={Boolean(rejectingId)} submitting={Boolean(activeId)} onCancel={() => setRejectingId(null)} onConfirm={reject} />
    </>
  )
}
