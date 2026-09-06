import { useState } from 'react'

type RejectReimbursementModalProps = {
  open: boolean
  submitting: boolean
  onCancel: () => void
  onConfirm: (reason: string) => Promise<void>
}

export function RejectReimbursementModal({ open, submitting, onCancel, onConfirm }: RejectReimbursementModalProps) {
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')

  if (!open) return null

  async function confirm() {
    if (!reason.trim()) {
      setError('Reason is required.')
      return
    }
    await onConfirm(reason.trim())
    setReason('')
    setError('')
  }

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal" role="dialog" aria-modal="true" aria-labelledby="reject-heading">
        <div className="section-kicker">Review decision</div>
        <h2 id="reject-heading">Reject reimbursement</h2>
        <label>
          Reason
          <textarea autoFocus rows={4} value={reason} onChange={(event) => { setReason(event.target.value); setError('') }} />
        </label>
        {error && <p className="feedback error" role="alert">{error}</p>}
        <div className="modal-actions">
          <button className="secondary-button" disabled={submitting} onClick={onCancel} type="button">Cancel</button>
          <button className="danger-button" disabled={submitting} onClick={() => void confirm()} type="button">
            {submitting ? 'Rejecting...' : 'Reject reimbursement'}
          </button>
        </div>
      </section>
    </div>
  )
}
