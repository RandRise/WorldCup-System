export interface Country {
  id: number;
  name: string;
  flag?: string | null;
}

export interface City {
  id: number;
  name: string;
  countryId: number;
}

export interface Stadium {
  id: number;
  name: string;
  cityId: number;
}

export interface WorldCup {
  id: number;
  year: string;
}

export interface Group {
  id: number;
  name: string;
  worldCupId: number;
}

export interface Team {
  id: number;
  countryId: number;
  countryName?: string | null;
  groupId: number;
  groupName?: string | null;
}

export interface Coach {
  id: number;
  name: string;
  teamId: number;
}

export interface Player {
  id: number;
  name: string;
  number: number;
  teamId: number;
  positionId: number;
  positionName?: string | null;
}

export interface PlayerPosition {
  id: number;
  name: string;
}

export interface Match {
  id: number;
  date: string;
  stadiumId: number;
  stadiumName?: string | null;
  teamOneId: number;
  teamOneName?: string | null;
  teamTwoId: number;
  teamTwoName?: string | null;
  teamOneScore?: number | null;
  teamTwoScore?: number | null;
  status: string;
  canBet: boolean;
}

export interface Standing {
  rank: number;
  teamId: number;
  teamName?: string | null;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
}

export interface Bet {
  id: number;
  userId: number;
  matchId: number;
  matchDate: string;
  teamOneName?: string | null;
  teamTwoName?: string | null;
  isDraw: boolean;
  predictedTeamId?: number | null;
  predictedOutcome?: string | null;
  isResolved: boolean;
  pointsEarned?: number | null;
  isActive: boolean;
}

export interface PlaceBetRequest {
  matchId: number;
  isDraw: boolean;
  teamId?: number | null;
}

export interface LeaderboardEntry {
  rank: number;
  userId: number;
  userName?: string | null;
  totalPoints: number;
  resolvedBets: number;
}

export interface LeaderboardSummary {
  rank?: number | null;
  userId: number;
  userName?: string | null;
  totalPoints: number;
  resolvedBets: number;
  activeBets: number;
}

export interface AddStadiumRequest {
  id: number;
  name: string;
  cityId: number;
}

export interface UpdateStadiumRequest {
  id: number;
  name: string;
  cityId: number;
}

export interface AddTeamRequest {
  countryId: number;
  groupId: number;
}

export interface UpdateTeamRequest {
  id: number;
  countryId: number;
  groupId: number;
}

export interface AddCoachRequest {
  name: string;
  teamId: number;
}

export interface UpdateCoachRequest {
  id: number;
  name: string;
  teamId: number;
}

export interface AddPlayerRequest {
  name: string;
  number: number;
  teamId: number;
  positionId: number;
}

export interface UpdatePlayerRequest {
  id: number;
  name: string;
  number: number;
  teamId: number;
  positionId: number;
}

export interface AddMatchRequest {
  teamOneId: number;
  teamTwoId: number;
  stadiumId: number;
  date: string;
}

export interface UpdateMatchRequest {
  id: number;
  teamOneId: number;
  teamTwoId: number;
  stadiumId: number;
  date: string;
}
