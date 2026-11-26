@TB-032 @identity-service @docker
Feature: identity-service access token JWT on login
  As a registered user
  I want a signed access token when login succeeds
  So that clients can call APIs without exposing secrets on failure

  Background:
    Given an empty PostgreSQL database is running
    And identity-service access token issuance is configured for BDD tests

  @S01 @FR-005
  Scenario: Successful login returns user id and a Bearer access token
    Given identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "token-s01@example.com" and password "ValidPass123456"
    When I send a login request with email "token-s01@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the login response has a non-empty id
    And the login response includes a Bearer access token with expires_in
    And the access token header uses algorithm RS256 and includes kid

  @S02 @FR-005 @BR-004
  Scenario: Access token lifetime matches configured seconds
    Given identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "token-s02@example.com" and password "ValidPass123456"
    When I send a login request with email "token-s02@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the access token lifetime is about 120 seconds

  @S03 @FR-005 @BR-008
  Scenario: Access token carries required user claims
    Given identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "token-s03@example.com" and password "ValidPass123456"
    When I send a login request with email "token-s03@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the access token includes required user claims for audience "teamboard"

  @S04 @FR-002
  Scenario: Login response does not leak signing key material
    Given identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "token-s04@example.com" and password "ValidPass123456"
    When I send a login request with email "token-s04@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the login response includes a Bearer access token with expires_in
    And the login response body does not contain a PEM private key

  @S05 @FR-002
  Scenario: Failed login does not return an access token
    Given identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "token-s05@example.com" and password "ValidPass123456"
    When I send a login request with email "token-s05@example.com" and password "WrongPass123456"
    Then the status code is 401
    And the error follows the standard format with code "INVALID_CREDENTIALS" and a trace id
    And the login response has no access_token property
    And no login error field contains a three-part JWT

  @S06 @NFR-007
  Scenario: Successful login records access token issue metrics
    Given identity-service metrics are being observed for issued access tokens
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "token-s06@example.com" and password "ValidPass123456"
    When I send a login request with email "token-s06@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the identity counter "access_tokens_issued_total" increased by at least 1