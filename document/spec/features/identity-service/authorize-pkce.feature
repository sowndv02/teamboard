@TB-042 @TB-052 @identity-service
Feature: identity-service OAuth authorize endpoint validation
  As a public OAuth client
  I want the authorize endpoint to validate PKCE and redirect_uri
  So that users are not sent to untrusted locations

  Background:
    Given identity-service rate limiting is disabled
    And identity-service oauth client registry is configured for BDD tests
    And identity-service authorization code grant is enabled for BDD tests
    And identity-service client "teamboard" has redirect uri "http://localhost:4200/callback" and allowed scopes:
      | scope  |
      | openid |

  @S02 @FR-012 @BR-005 @docker
  Scenario: Untrusted redirect_uri is rejected without redirecting to it
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I GET the authorize endpoint for client "teamboard" with redirect_uri "http://evil.example/callback" state "state-abc" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    Then the status code is 400
    And the response has no Location header containing "http://evil.example/callback"

  @S03 @FR-012 @docker
  Scenario: Missing state is rejected without redirect
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I GET the authorize endpoint for client "teamboard" with redirect_uri "http://localhost:4200/callback" without state scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    Then the status code is 400
    And the response has no Location header containing "http://localhost:4200/callback"

  @S03b @FR-012 @docker
  Scenario: Missing code_challenge is rejected without redirect
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I GET the authorize endpoint for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-xyz" scope "openid" without pkce challenge method "S256"
    Then the status code is 400
    And the response has no Location header containing "http://localhost:4200/callback"

  @S03c @FR-012 @docker
  Scenario: Requested scope outside allowed scopes is rejected without redirect
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I GET the authorize endpoint for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-scope" scope "admin" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    Then the status code is 400
    And the response has no Location header containing "http://localhost:4200/callback"

  @S07 @FR-012 @docker
  Scenario: Unknown client_id is rejected without redirect
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I GET the authorize endpoint for client "unknown-bdd-client" with redirect_uri "http://localhost:4200/callback" state "state-1" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    Then the status code is 400
    And the response has no Location header containing "http://localhost:4200/callback"

  @S01 @TB-052 @FR-012 @FR-019 @docker
  Scenario: Public client authorize with login redirects authorization code and state
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "authz-s01@example.com" and password "ValidPass123456"
    When I GET the authorize endpoint for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-happy" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    And I submit the identity login page for the pending authorize request with email "authz-s01@example.com" and password "ValidPass123456"
    Then the status code is 302
    And the authorize response redirects to "http://localhost:4200/callback" with state "state-happy" and an authorization code

  @S01b @TB-052 @FR-012 @FR-019 @docker
  Scenario: Confidential client with registered redirect_uri receives authorization code after login
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service has confidential oauth client "authz-confidential" with secret "bdd-confidential-secret-32charsxx"
    And identity-service client "authz-confidential" has redirect uri "http://localhost:4200/callback" and allowed scopes:
      | scope  |
      | openid |
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "authz-s01b@example.com" and password "ValidPass123456"
    When I GET the authorize endpoint for client "authz-confidential" with redirect_uri "http://localhost:4200/callback" state "state-conf" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    And I submit the identity login page for the pending authorize request with email "authz-s01b@example.com" and password "ValidPass123456"
    Then the status code is 302
    And the authorize response redirects to "http://localhost:4200/callback" with state "state-conf" and an authorization code

  @S04 @TB-052 @FR-019 @BR-003 @docker
  Scenario: Wrong password on the login page does not redirect to the client
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "authz-s04@example.com" and password "ValidPass123456"
    When I GET the authorize endpoint for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-wrong-pw" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    And I submit the identity login page for the pending authorize request with email "authz-s04@example.com" and password "WrongPass123456"
    Then the status code is 200
    And the response has no Location header containing "http://localhost:4200/callback"

  @S05 @TB-052 @FR-003 @FR-019 @docker
  Scenario: Locked account on the login page does not redirect to the client
    Given an empty PostgreSQL database is running
    And identity-service lockout is configured with max failures 5 and lockout duration 60 seconds
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "authz-s05@example.com" and password "ValidPass123456"
    And the email and IP pair for "authz-s05@example.com" is locked from failed logins
    When I GET the authorize endpoint for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-lock" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    And I submit the identity login page for the pending authorize request with email "authz-s05@example.com" and password "ValidPass123456"
    Then the status code is 401
    And the response has no Location header containing "http://localhost:4200/callback"
    And the response has a Retry-After header greater than 0

  @S06 @TB-052 @FR-019 @docker
  Scenario: Valid IdP session skips the login form and redirects with a code
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "authz-s06@example.com" and password "ValidPass123456"
    When I GET the authorize endpoint for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-first" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    And I submit the identity login page for the pending authorize request with email "authz-s06@example.com" and password "ValidPass123456"
    And I GET the authorize endpoint for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-second" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256"
    Then the status code is 302
    And the authorize response redirects to "http://localhost:4200/callback" with state "state-second" and an authorization code

  @S08 @TB-052 @NFR-010 @docker
  Scenario: Login page response includes clickjacking protection header
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I GET the identity login page
    Then the status code is 200
    And the response has Content-Security-Policy frame-ancestors none