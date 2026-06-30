export type CallType = 'OneToOne' | 'Group';
export type CallStatus = 'Initiated' | 'Ongoing' | 'Ended' | 'Missed' | 'Declined';

export interface CallParticipantInfo {
  userId: string;
  fullName: string;
  profilePictureUrl?: string;
  status: string;
  joinedAt?: string;
}

export interface CallHistory {
  id: string;
  initiatedById: string;
  type: CallType;
  status: CallStatus;
  startedAt?: string;
  endedAt?: string;
  durationSeconds?: number;
  participants: CallParticipantInfo[];
  isIncoming: boolean;
}
