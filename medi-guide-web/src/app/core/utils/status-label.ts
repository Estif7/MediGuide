const bookingStatusMapEn: Record<number, string> = {
  0: 'Pending payment',
  1: 'Paid',
  2: 'Assigned',
  3: 'In progress',
  4: 'Completed',
  5: 'Cancelled',
};

const bookingStatusMapAm: Record<number, string> = {
  0: 'ክፍያ በመጠበቅ ላይ',
  1: 'ተከፍሏል',
  2: 'ባለሙያ ተመድቧል',
  3: 'በሂደት ላይ',
  4: 'ተጠናቋል',
  5: 'ተሰርዟል',
};

const responseTimeMapEn: Record<number, string> = {
  0: 'Priority (24h)',
  1: 'Expedited (2 days)',
  2: 'Standard (5 days)',
};

const responseTimeMapAm: Record<number, string> = {
  0: 'ቅድሚያ (24 ሰዓት)',
  1: 'አስቸኳይ (2 ቀናት)',
  2: 'መደበኛ (5 ቀናት)',
};

export function bookingStatusLabel(status: number, isAmharic = false): string {
  const map = isAmharic ? bookingStatusMapAm : bookingStatusMapEn;
  return map[status] ?? `Status ${status}`;
}

export function responseTimeLabel(value: number, isAmharic = false): string {
  const map = isAmharic ? responseTimeMapAm : responseTimeMapEn;
  return map[value] ?? `Response ${value}`;
}