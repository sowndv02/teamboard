@TB-038 @identity-service
Feature: identity-service logout revokes refresh session
  As a client application
  I want to revoke my refresh token on logout
  So that the refresh session cannot be renewed after I sign out

  Background:
    Given identity-service rate limiting is disabled
    And identity-service access token issuance is configured for BDD tests

  @S01 @FR-011 @docker
  Scenario: Successful logout returns no content and blocks refresh
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "logout-s01@example.com" and password "ValidPass123456"
    When I send a login request with email "logout-s01@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login refresh token for rotation checks
    When I send a logout request with the stored refresh token
    Then the status code is 204
    When I send a refresh request with the stored refresh token
    Then the status code is 401

  @S02 @FR-011 @docker
  Scenario: Logout rejects an invalid refresh token
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    When I send a logout request with refresh token "not-a-valid-refresh-token"
    Then the status code is 401

  @S03 @FR-011 @docker
  Scenario: Logout again after session revoked is rejected
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "logout-s03@example.com" and password "ValidPass123456"
    When I send a login request with email "logout-s03@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login refresh token for rotation checks
    When I send a logout request with the stored refresh token
    Then the status code is 204
    When I send a logout request with the stored refresh token
    Then the status code is 401