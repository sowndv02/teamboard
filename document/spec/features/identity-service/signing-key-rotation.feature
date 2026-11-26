@TB-035 @identity-service
Feature: identity-service signing key rotation
  As a platform operator
  I want to rotate RS256 signing keys without invalidating in-flight access tokens
  So that resource servers can verify tokens during a controlled rotation window

  Background:
    Given identity-service rate limiting is disabled
    And identity-service rotation uses an isolated signing keys file

  @S01 @FR-008 @docker
  Scenario: After rotate start JWKS exposes pending key and login still uses active kid
    Given an empty PostgreSQL database is running
    And identity-service access token issuance is configured for BDD tests
    And identity-service signing keys file points to the rotation test file
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "rotate-s01@example.com" and password "ValidPass123456"
    When I run signing key rotate start
    And I send GET to "/.well-known/jwks.json"
    Then the status code is 200
    And the JWKS document includes public key with kid "test-rotate-pending-1"
    And the JWKS document includes public key with kid "test-active-1"
    When I send a login request with email "rotate-s01@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the access token header kid is "test-active-1"

  @S02 @FR-008 @docker
  Scenario: After activate login uses new kid and prior token keeps its kid
    Given an empty PostgreSQL database is running
    And identity-service access token issuance is configured for BDD tests
    And identity-service signing keys file points to the rotation test file
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "rotate-s02@example.com" and password "ValidPass123456"
    And I send a login request with email "rotate-s02@example.com" and password "ValidPass123456"
    And the status code is 200
    And I store the current login access token for rotation checks
    When I run signing key rotate start
    And I advance rotation fake time by 11 minutes
    And I run signing key rotate activate
    When I send a login request with email "rotate-s02@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the access token header kid is "test-rotate-active-2"
    And the stored access token header kid is "test-active-1"

  @S03 @FR-008
  Scenario: Activate before JWKS publish window fails
    Given rotation fake UTC time is "2026-01-01T00:00:00Z"
    When I run signing key rotate start
    And I run signing key rotate activate
    Then the rotation command exit code is not 0

  @S04 @FR-008
  Scenario: Retire before minimum retiring duration fails
    Given rotation fake UTC time is "2026-01-01T00:00:00Z"
    When I run signing key rotate start
    And I advance rotation fake time by 11 minutes
    And I run signing key rotate activate
    When I run signing key rotate retire
    Then the rotation command exit code is not 0

  @S06 @FR-008
  Scenario: Retire after minimum retiring duration removes retiring kid from JWKS
    Given rotation fake UTC time is "2026-01-01T00:00:00Z"
    And identity-service signing keys file points to the rotation test file
    And identity-service is running for JWKS without a database
    When I run signing key rotate start
    And I advance rotation fake time by 11 minutes
    And I run signing key rotate activate
    And I advance rotation fake time by 25 hours
    And I run signing key rotate retire
    And I send GET to "/.well-known/jwks.json"
    Then the status code is 200
    And the JWKS document does not include public key with kid "test-active-1"
    And the JWKS document includes public key with kid "test-rotate-active-2"

  @S05 @FR-008 @docker
  Scenario: Without rotation JWKS and login use a single active key
    Given an empty PostgreSQL database is running
    And identity-service access token issuance is configured for BDD tests
    And identity-service signing keys file points to the rotation test file
    And identity-service is configured to use that database and applies migrations on startup
    When I send GET to "/.well-known/jwks.json"
    Then the status code is 200
    And the JWKS document includes exactly 1 public key
    When a user is registered with email "rotate-s05@example.com" and password "ValidPass123456"
    And I send a login request with email "rotate-s05@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the access token header kid is "test-active-1"