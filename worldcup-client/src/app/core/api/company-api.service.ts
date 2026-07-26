import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Company,
  CompanyMember,
  CompanyMine,
  CreateCompanyRequest,
  JoinCompanyRequest,
  JoinCompanyResponse,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class CompanyApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getMine(): Promise<CompanyMine> {
    return firstValueFrom(this.http.get<CompanyMine>(`${this.baseUrl}/Company/Mine`));
  }

  join(request: JoinCompanyRequest): Promise<JoinCompanyResponse> {
    return firstValueFrom(
      this.http.post<JoinCompanyResponse>(`${this.baseUrl}/Company/Join`, request),
    );
  }

  rotateInviteCode(companyId?: number): Promise<Company> {
    const body = companyId != null ? { companyId } : {};
    return firstValueFrom(
      this.http.post<Company>(`${this.baseUrl}/Company/RotateInviteCode`, body),
    );
  }

  getMembers(companyId?: number): Promise<CompanyMember[]> {
    let params = new HttpParams();
    if (companyId != null) {
      params = params.set('companyId', String(companyId));
    }
    return firstValueFrom(
      this.http.get<CompanyMember[]>(`${this.baseUrl}/Company/Members`, { params }),
    );
  }

  create(request: CreateCompanyRequest): Promise<Company> {
    return firstValueFrom(
      this.http.post<Company>(`${this.baseUrl}/Company/Create`, request),
    );
  }

  getAll(): Promise<Company[]> {
    return firstValueFrom(this.http.get<Company[]>(`${this.baseUrl}/Company/GetAll`));
  }
}
