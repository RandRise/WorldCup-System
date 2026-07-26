import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CompanyApiService } from './company-api.service';
import { environment } from '../../../environments/environment';

describe('CompanyApiService', () => {
  let api: CompanyApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CompanyApiService],
    });

    api = TestBed.inject(CompanyApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getMine calls Company/Mine', async () => {
    const promise = api.getMine();

    const request = httpMock.expectOne(`${environment.apiUrl}/Company/Mine`);
    expect(request.request.method).toBe('GET');
    request.flush({
      company: null,
      isCompanyAdmin: false,
      becameCompanyAdmin: false,
    });

    const mine = await promise;
    expect(mine.company).toBeNull();
  });

  it('join posts invite code and returns company + optional token', async () => {
    const promise = api.join({ inviteCode: 'ABCD2345' });

    const request = httpMock.expectOne(`${environment.apiUrl}/Company/Join`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ inviteCode: 'ABCD2345' });
    request.flush({
      company: {
        id: 1,
        name: 'Acme',
        slug: null,
        inviteCode: 'ABCD2345',
        createdAt: '2026-07-23T00:00:00Z',
        memberCount: 1,
      },
      becameCompanyAdmin: true,
      isCompanyAdmin: true,
      token: 'new-jwt',
      expiration: '2099-01-01T00:00:00Z',
    });

    const result = await promise;
    expect(result.company.name).toBe('Acme');
    expect(result.becameCompanyAdmin).toBeTrue();
    expect(result.token).toBe('new-jwt');
  });

  it('rotateInviteCode posts optional companyId for Admin override', async () => {
    const promise = api.rotateInviteCode(9);

    const request = httpMock.expectOne(`${environment.apiUrl}/Company/RotateInviteCode`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ companyId: 9 });
    request.flush({
      id: 9,
      name: 'Globex',
      slug: null,
      inviteCode: 'NEWCODE1',
      createdAt: '2026-07-23T00:00:00Z',
      memberCount: 3,
    });

    const company = await promise;
    expect(company.inviteCode).toBe('NEWCODE1');
  });

  it('getMembers includes companyId query when provided', async () => {
    const promise = api.getMembers(5);

    const request = httpMock.expectOne(
      (req) => req.url === `${environment.apiUrl}/Company/Members` && req.params.get('companyId') === '5',
    );
    expect(request.request.method).toBe('GET');
    request.flush([{ id: 1, name: 'Ada', email: 'ada@test' }]);

    const members = await promise;
    expect(members.length).toBe(1);
    expect(members[0].name).toBe('Ada');
  });
});
