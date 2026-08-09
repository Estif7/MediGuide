const bookingStatusMap: Record<number, string> = {
  0: 'Pending payment',
  1: 'Paid',
  2: 'Assigned',
  3: 'In progress',
  4: 'Completed',
  5: 'Cancelled',
};

const responseTimeMap: Record<number, string> = {
  0: 'Priority (24h)',
  1: 'Expedited (2 days)',
  2: 'Standard (5 days)',
};

export function bookingStatusLabel(status: number): string {
  return bookingStatusMap[status] ?? `Status ${status}`;
}

export function responseTimeLabel(value: number): string {
  return responseTimeMap[value] ?? `Response ${value}`;
}