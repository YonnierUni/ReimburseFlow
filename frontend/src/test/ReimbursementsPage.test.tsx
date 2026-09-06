import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from '../App'
import { apiDocsUrl } from '../api/reimbursementsApi'

const reimbursement = {
  id: 'r-1',
  employeeId: 'employee-1',
  receiptNumber: 'receipt-1',
  expenseDate: '2026-09-05',
  category: 'Food',
  description: 'Client lunch',
  amount: 25.5,
  status: 'Pending',
  rejectionReason: null,
  createdAt: '2026-09-05T10:00:00Z',
  updatedAt: null,
}

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve(new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }))
}

describe('ReimbursementsPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.stubGlobal('crypto', { randomUUID: vi.fn(() => 'same-attempt-key') })
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse([reimbursement])))
  })

  it('renders the required form fields', async () => {
    render(<App />)
    expect(screen.getByLabelText('Employee ID')).toBeInTheDocument()
    expect(screen.getByLabelText('Receipt Number')).toBeInTheDocument()
    expect(screen.getByLabelText('Expense Date')).toBeInTheDocument()
    expect(screen.getAllByLabelText('Category')[0]).toBeInTheDocument()
    expect(screen.getByLabelText('Description')).toBeInTheDocument()
    expect(screen.getByLabelText('Amount')).toBeInTheDocument()
    await screen.findByText('employee-1')
  })

  it('derives the Swagger URL from the API configuration', async () => {
    render(<App />)

    expect(screen.getByRole('link', { name: /Open API docs/ })).toHaveAttribute('href', apiDocsUrl)
    expect(apiDocsUrl).toBe('http://localhost:5087/swagger')
    await screen.findByText('employee-1')
  })

  it('shows loading and then reimbursements with pending actions', async () => {
    let resolveRequest!: (response: Response) => void
    vi.stubGlobal('fetch', vi.fn(() => new Promise<Response>((resolve) => { resolveRequest = resolve })))
    render(<App />)
    expect(screen.getByText('Loading reimbursements...')).toBeInTheDocument()
    resolveRequest(await jsonResponse([reimbursement]))
    expect(await screen.findByText('employee-1')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Approve' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Reject' })).toBeInTheDocument()
  })

  it('shows an empty state', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse([])))
    render(<App />)
    expect(await screen.findByText('No reimbursements found.')).toBeInTheDocument()
  })

  it('blocks invalid amount without submitting', async () => {
    const fetchMock = vi.fn(() => jsonResponse([reimbursement]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    await screen.findByText('employee-1')
    await userEvent.type(screen.getByLabelText('Employee ID'), 'employee-2')
    await userEvent.type(screen.getByLabelText('Receipt Number'), 'receipt-2')
    await userEvent.type(screen.getByLabelText('Expense Date'), '09/05/2026')
    await userEvent.type(screen.getByLabelText('Description'), 'Taxi')
    await userEvent.clear(screen.getByLabelText('Amount'))
    await userEvent.type(screen.getByLabelText('Amount'), '0')
    await userEvent.click(screen.getByRole('button', { name: 'Create reimbursement' }))
    expect(screen.getByLabelText('Amount')).toBeInvalid()
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('creates a reimbursement with an idempotency key', async () => {
    const fetchMock = vi.fn()
      .mockImplementationOnce(() => jsonResponse([reimbursement]))
      .mockImplementationOnce(() => jsonResponse(reimbursement, 201))
      .mockImplementationOnce(() => jsonResponse([reimbursement]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    await screen.findByText('employee-1')
    await userEvent.type(screen.getByLabelText('Employee ID'), 'employee-2')
    await userEvent.type(screen.getByLabelText('Receipt Number'), 'receipt-2')
    fireEvent.change(screen.getByLabelText('Expense Date'), { target: { value: '2026-09-05' } })
    await userEvent.type(screen.getByLabelText('Description'), 'Taxi')
    await userEvent.clear(screen.getByLabelText('Amount'))
    await userEvent.type(screen.getByLabelText('Amount'), '12')
    await userEvent.click(screen.getByRole('button', { name: 'Create reimbursement' }))
    await waitFor(() => expect(screen.getByText('Reimbursement created successfully.')).toBeInTheDocument())
    expect(fetchMock.mock.calls[1][1]).toEqual(expect.objectContaining({ headers: expect.objectContaining({ 'Idempotency-Key': 'same-attempt-key' }) }))
  })

  it('uses API filters and hides actions for approved items', async () => {
    const approved = { ...reimbursement, status: 'Approved' }
    const fetchMock = vi.fn(() => jsonResponse([approved]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    await screen.findByText('employee-1')
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reject' })).not.toBeInTheDocument()
    fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'Approved' } })
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('status=Approved'), expect.anything()))
  })

  it('hides actions for rejected items', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse([{ ...reimbursement, status: 'Rejected' }])))
    render(<App />)
    await screen.findByText('employee-1')
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reject' })).not.toBeInTheDocument()
  })

  it('approves a pending reimbursement and refreshes the list', async () => {
    const fetchMock = vi.fn()
      .mockImplementationOnce(() => jsonResponse([reimbursement]))
      .mockImplementationOnce(() => jsonResponse({ ...reimbursement, status: 'Approved' }))
      .mockImplementationOnce(() => jsonResponse([{ ...reimbursement, status: 'Approved' }]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    await screen.findByText('employee-1')
    await userEvent.click(screen.getByRole('button', { name: 'Approve' }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3))
    expect(fetchMock.mock.calls[1][0]).toContain('/approve')
  })

  it('clears action errors when a new action starts and after it succeeds', async () => {
    const fetchMock = vi.fn()
      .mockImplementationOnce(() => jsonResponse([reimbursement]))
      .mockImplementationOnce(() => jsonResponse({ detail: 'Approval failed.', status: 409 }, 409))
      .mockImplementationOnce(() => jsonResponse({ ...reimbursement, status: 'Approved' }))
      .mockImplementationOnce(() => jsonResponse([{ ...reimbursement, status: 'Approved' }]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    await screen.findByText('employee-1')

    await userEvent.click(screen.getByRole('button', { name: 'Approve' }))
    expect(await screen.findByText('Approval failed.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Approve' }))
    await waitFor(() => expect(screen.queryByText('Approval failed.')).not.toBeInTheDocument())
  })

  it('clears action errors when filters change', async () => {
    const fetchMock = vi.fn()
      .mockImplementationOnce(() => jsonResponse([reimbursement]))
      .mockImplementationOnce(() => jsonResponse({ detail: 'Approval failed.', status: 409 }, 409))
      .mockImplementation(() => jsonResponse([reimbursement]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    await screen.findByText('employee-1')
    await userEvent.click(screen.getByRole('button', { name: 'Approve' }))
    expect(await screen.findByText('Approval failed.')).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'Approved' } })
    expect(screen.queryByText('Approval failed.')).not.toBeInTheDocument()
    await screen.findByText('employee-1')
  })

  it('rejects a pending reimbursement after a reason is provided', async () => {
    const fetchMock = vi.fn()
      .mockImplementationOnce(() => jsonResponse([reimbursement]))
      .mockImplementationOnce(() => jsonResponse({ ...reimbursement, status: 'Rejected' }))
      .mockImplementationOnce(() => jsonResponse([{ ...reimbursement, status: 'Rejected' }]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    await screen.findByText('employee-1')
    await userEvent.click(screen.getByRole('button', { name: 'Reject' }))
    await userEvent.type(screen.getByRole('textbox', { name: 'Reason' }), 'Unreadable receipt')
    await userEvent.click(screen.getByRole('button', { name: 'Reject reimbursement' }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3))
    expect(fetchMock.mock.calls[1][0]).toContain('/reject')
  })

  it('uses the category filter in the API request', async () => {
    const fetchMock = vi.fn(() => jsonResponse([reimbursement]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    await screen.findByText('employee-1')
    fireEvent.change(screen.getAllByLabelText('Category')[1], { target: { value: 'Food' } })
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('category=Food'), expect.anything()))
  })

  it('requires a reason before rejecting', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse([reimbursement])))
    render(<App />)
    await screen.findByText('employee-1')
    await userEvent.click(screen.getByRole('button', { name: 'Reject' }))
    await userEvent.click(screen.getByRole('button', { name: 'Reject reimbursement' }))
    expect(screen.getByText('Reason is required.')).toBeInTheDocument()
  })

  it('shows a ProblemDetails error', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse({ status: 500, title: 'Server error', detail: 'Service unavailable.' }, 500)))
    render(<App />)
    expect(await screen.findByText('Service unavailable.')).toBeInTheDocument()
  })

  it('shows a friendly network error', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new TypeError('Failed to fetch'))))
    render(<App />)
    expect(await screen.findByText('Could not connect to the server.')).toBeInTheDocument()
  })

  it('aborts the previous list request and ignores its stale response', async () => {
    let resolveOld!: (response: Response) => void
    let oldSignal!: AbortSignal
    const fetchMock = vi.fn()
      .mockImplementationOnce((_url: string, options: RequestInit) => {
        oldSignal = options.signal as AbortSignal
        return new Promise<Response>((resolve) => { resolveOld = resolve })
      })
      .mockImplementation(() => jsonResponse([{ ...reimbursement, employeeId: 'approved-employee' }]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'Approved' } })
    expect(await screen.findByText('approved-employee')).toBeInTheDocument()
    expect(oldSignal.aborted).toBe(true)
    resolveOld(await jsonResponse([{ ...reimbursement, employeeId: 'stale-pending' }]))
    await waitFor(() => expect(screen.queryByText('stale-pending')).not.toBeInTheDocument())
  })

  it('does not show a connection error for an aborted request', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new DOMException('Aborted', 'AbortError'))))
    render(<App />)
    await waitFor(() => expect(screen.queryByText('Could not connect to the server.')).not.toBeInTheDocument())
    expect(screen.queryByText('Could not connect to the server.')).not.toBeInTheDocument()
  })

  it('reuses the idempotency key for an identical retry and replaces it after editing', async () => {
    const randomUUID = vi.fn().mockReturnValueOnce('first-key').mockReturnValueOnce('second-key')
    vi.stubGlobal('crypto', { randomUUID })
    const fetchMock = vi.fn()
      .mockImplementationOnce(() => jsonResponse([reimbursement]))
      .mockImplementationOnce(() => Promise.reject(new TypeError('Failed to fetch')))
      .mockImplementationOnce(() => Promise.reject(new TypeError('Failed to fetch')))
      .mockImplementationOnce(() => jsonResponse(reimbursement, 201))
      .mockImplementationOnce(() => jsonResponse([reimbursement]))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)
    await screen.findByText('employee-1')
    await fillForm()

    await userEvent.click(screen.getByRole('button', { name: 'Create reimbursement' }))
    expect(await screen.findByText('Could not connect to the server.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Create reimbursement' }))
    expect(fetchMock.mock.calls[2][1]).toEqual(expect.objectContaining({ headers: expect.objectContaining({ 'Idempotency-Key': 'first-key' }) }))

    await userEvent.type(screen.getByLabelText('Description'), ' edited')
    await userEvent.click(screen.getByRole('button', { name: 'Create reimbursement' }))
    expect(fetchMock.mock.calls[3][1]).toEqual(expect.objectContaining({ headers: expect.objectContaining({ 'Idempotency-Key': 'second-key' }) }))
  })

  async function fillForm() {
    await userEvent.type(screen.getByLabelText('Employee ID'), 'employee-2')
    await userEvent.type(screen.getByLabelText('Receipt Number'), 'receipt-2')
    fireEvent.change(screen.getByLabelText('Expense Date'), { target: { value: '2026-09-05' } })
    await userEvent.type(screen.getByLabelText('Description'), 'Taxi')
    await userEvent.clear(screen.getByLabelText('Amount'))
    await userEvent.type(screen.getByLabelText('Amount'), '12')
  }
})