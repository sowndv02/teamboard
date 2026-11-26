@TB-042 @identity-service
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