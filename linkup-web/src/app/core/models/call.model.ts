export type CallType = 'OneToOne' | 'Group';
export type CallStatus = 'Initiated' | 'Ongoing' | 'Ended' | 'Missed' | 'Declined';

export interface CallParticipant {
  userId: string;
  fullName: string;
  profilePictureUrl?: string;
  status: string;
  joinedAt?: string;
}

export interface CallHistoryItem {
  id: string;
  initiatedById: string;
  type: CallType;
  status: CallStatus;
  startedAt?: string;
  endedAt?: string;
  durationSeconds?: number;
  participants: CallParticipant[];
  isIncoming: boolean;
}
