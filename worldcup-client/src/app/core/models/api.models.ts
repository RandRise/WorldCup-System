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
  stage?: MatchStage;
  stageName?: string;
  stadiumId: number;
  stadiumName?: string | null;
  teamOneId?: number | null;
  teamOneName?: string | null;
  teamTwoId?: number | null;
  teamTwoName?: string | null;
  feederMatchOneId?: number | null;
  feederMatchTwoId?: number | null;
  teamOneScore?: number | null;
  teamTwoScore?: number | null;
  status: string;
  canBet: boolean;
  externalMatchId?: string | null;
}

/** Mirrors Data.Entities.MatchStage */
export const MatchStages = {
  Group: 0,
  RoundOf16: 1,
  QuarterFinal: 2,
  SemiFinal: 3,
  ThirdPlace: 4,
  Final: 5,
  RoundOf32: 6,
} as const;

export type MatchStage = (typeof MatchStages)[keyof typeof MatchStages];

export const MATCH_STAGE_OPTIONS: { value: MatchStage; label: string }[] = [
  { value: MatchStages.Group, label: 'Group' },
  { value: MatchStages.RoundOf32, label: 'Round of 32' },
  { value: MatchStages.RoundOf16, label: 'Round of 16' },
  { value: MatchStages.QuarterFinal, label: 'Quarter-final' },
  { value: MatchStages.SemiFinal, label: 'Semi-final' },
  { value: MatchStages.ThirdPlace, label: 'Third place' },
  { value: MatchStages.Final, label: 'Final' },
];

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
  stage: MatchStage;
}

export interface UpdateMatchRequest {
  id: number;
  teamOneId?: number | null;
  teamTwoId?: number | null;
  stadiumId: number;
  date: string;
  stage: MatchStage;
}

export interface MatchGoal {
  id: number;
  playerId: number;
  playerName?: string | null;
  minute: number;
  isOwnGoal: boolean;
}

export interface MatchCard {
  id: number;
  playerId: number;
  playerName?: string | null;
  minute: number;
  type?: string | null;
}

export interface MatchTeamStats {
  teamStatsId: number;
  teamId: number;
  teamName?: string | null;
  possession: number;
  shots: number;
  shotsOnTarget: number;
  score: number;
  goals: MatchGoal[];
  cards: MatchCard[];
}

export interface MatchDetail extends Match {
  teamOneStats?: MatchTeamStats | null;
  teamTwoStats?: MatchTeamStats | null;
}

export interface BracketRound {
  stage: MatchStage;
  stageName: string;
  matches: Match[];
}

export interface Bracket {
  worldCupId: number;
  rounds: BracketRound[];
}

export interface GenerateBracketRequest {
  worldCupId: number;
  stadiumId: number;
  firstKickoff: string;
}

export interface GenerateBracketResult {
  matchesCreated: number;
  message: string;
  bracket: Bracket;
}

export interface Goal {
  id: number;
  matchId: number;
  teamId: number;
  playerId: number;
  playerName?: string | null;
  minute: number;
  isOwnGoal: boolean;
}

export interface Card {
  id: number;
  matchId: number;
  teamId: number;
  playerId: number;
  playerName?: string | null;
  minute: number;
  type?: number | null;
  typeName?: string | null;
}

export interface TeamStats {
  teamStatsId: number;
  matchId: number;
  teamId: number;
  teamName?: string | null;
  possession: number;
  shots: number;
  shotsOnTarget: number;
  score: number;
}

export interface AddGoalRequest {
  matchId: number;
  teamId: number;
  playerId: number;
  minute: number;
  isOwnGoal: boolean;
}

export interface AddCardRequest {
  matchId: number;
  teamId: number;
  playerId: number;
  minute: number;
  cardType: number;
}

export interface UpdateTeamStatsRequest {
  matchId: number;
  teamId: number;
  possession: number;
  shots: number;
  shotsOnTarget: number;
}

export interface BetResolveUser {
  userId: number;
  userName?: string | null;
  predictedOutcome?: string | null;
  pointsAwarded: number;
  previousPoints?: number | null;
}

export interface ResolveBetsResult {
  matchId: number;
  resolvedCount: number;
  message: string;
  userBreakdown: BetResolveUser[];
}

export interface ResolveBetsWorldCupResult {
  worldCupId: number;
  matchesProcessed: number;
  totalResolvedCount: number;
  message: string;
  matchResults: ResolveBetsResult[];
}

export interface SetExternalMatchIdRequest {
  matchId: number;
  externalMatchId: string;
}

export interface SyncMatchResult {
  matchId: number;
  externalMatchId?: string | null;
  applied: boolean;
  scoreChanged: boolean;
  teamOneScore: number;
  teamTwoScore: number;
  betsResolved: number;
  message: string;
  warning?: string | null;
  resolveResult?: ResolveBetsResult | null;
}

export interface SyncFinishedResults {
  worldCupId: number;
  matchesAttempted: number;
  matchesApplied: number;
  totalBetsResolved: number;
  message: string;
  results: SyncMatchResult[];
}

export interface LiveMatchSnapshot {
  matchId: number;
  status: string;
  teamOneScore: number;
  teamTwoScore: number;
  currentMinute?: number | null;
}

export interface LiveEventSnapshot {
  matchId: number;
  eventType: string;
  minute: number;
  playerName?: string | null;
  teamName?: string | null;
}

export interface LiveSnapshot {
  matches: LiveMatchSnapshot[];
  recentEvents: LiveEventSnapshot[];
}
