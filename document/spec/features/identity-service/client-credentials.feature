@TB-041 @identity-service
Feature: identity-service OAuth client credentials grant
  As a confidential service client
  I want to obtain a machine access token using client_id and client_secret
  So that service-to-service calls can authenticate without a user

  Background:
    Given identity-service rate limiting is disabled
    And identity-service access token issuance is configured for BDD tests
    And identity-service oauth client registry is configured for BDD tests
    And identity-service client credentials grant is enabled for BDD tests

  @S01 @FR-015 @docker
  Scenario: Confidential client receives a machine access token for an allowed scope
    Given an empty PostgreSQL database is running
    And identity-service has confidential oauth client "bdd-cc" with secret "CcSecret-Test-Value-12" allows scopes:
      | scope    |
      | api.read |
    And identity-service is configured to use that database and applies migrations on startup
    When I request a client credentials token for client "bdd-cc" with secret "CcSecret-Test-Value-12" and scope "api.read"
    Then the status code is 200
    And the token response includes a Bearer access token with expires_in
    And the access token is a machine token for client "bdd-cc" with scope "api.read" and audience "teamboard"

  @S02 @BR-008 @docker
  Scenario: Machine access token is not a user token
    Given an empty PostgreSQL database is running
    And identity-service has confidential oauth client "bdd-machine" with secret "MachineSecret-TestValue-12" allows scopes:
      | scope    |
      | api.read |
    And identity-service is configured to use that database and applies migrations on startup
    When I request a client credentials token for client "bdd-machine" with secret "MachineSecret-TestValue-12" and scope "api.read"
    Then the status code is 200
    And the access token is not a user access token

  @S03b @FR-015 @docker
  Scenario: Missing scope is rejected
    Given an empty PostgreSQL database is running
    And identity-service has confidential oauth client "bdd-noscope" with secret "NoScopeSecret-Test-12" allows scopes:
      | scope    |
      | api.read |
    And identity-service is configured to use that database and applies migrations on startup
    When I request a client credentials token for client "bdd-noscope" with secret "NoScopeSecret-Test-12" without a scope
    Then the status code is 400
    And the oauth token response error is "invalid_scope"
    And the token response has no access_token

  @S03 @FR-015 @docker
  Scenario: Requested scope outside allowed scopes is rejected
    Given an empty PostgreSQL database is running
    And identity-service has confidential oauth client "bdd-scope" with secret "ScopeSecret-TestValue-12" allows scopes:
      | scope    |
      | api.read |
    And identity-service is configured to use that database and applies migrations on startup
    When I request a client credentials token for client "bdd-scope" with secret "ScopeSecret-TestValue-12" and scope "admin"
    Then the status code is 400
    And the oauth token response error is "invalid_scope"
    And the token response has no access_token

  @S04 @FR-015 @docker
  Scenario: Invalid client credentials are rejected without leaking the secret
    Given an empty PostgreSQL database is running
    And identity-service has confidential oauth client "bdd-bad" with secret "GoodSecret-Test-Value-12" allows scopes:
      | scope    |
      | api.read |
    And identity-service is configured to use that database and applies migrations on startup
    When I request a client credentials token for client "bdd-bad" with secret "WrongSecret-Value-99" and scope "api.read"
    Then the status code is 401
    And the oauth token response error is "invalid_client"
    And the token response has no access_token
    And the response body does not contain "WrongSecret-Value-99"

  @S05 @BR-007 @docker
  Scenario: Client credentials handling does not log the client secret
    Given an empty PostgreSQL database is running
    And identity-service has confidential oauth client "cc-log" with secret "DoNotLogCcSecret-Value-99" allows scopes:
      | scope    |
      | api.read |
    And identity-service log capture is enabled
    And identity-service is configured to use that database and applies migrations on startup
    When I request a client credentials token for client "cc-log" with secret "DoNotLogCcSecret-Value-99" and scope "api.read"
    Then the status code is not 404
    And captured logs contain no oauth client secret

  @S06 @FR-015 @docker
  Scenario: Public client cannot use the client credentials grant
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I request a client credentials token for client "teamboard" with secret "ignored" and scope "openid"
    Then the status code is 401
    And the oauth token response error is "unauthorized_client"
    And the token response has no access_token