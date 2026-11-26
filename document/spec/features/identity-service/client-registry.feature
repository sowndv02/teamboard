@TB-040 @identity-service
Feature: identity-service OAuth client registry
  As an operator
  I want OAuth clients to be stored from configuration without a registration API
  So that PKCE and client credentials can validate clients in later slices

  Background:
    Given identity-service rate limiting is disabled
    And identity-service access token issuance is configured for BDD tests
    And identity-service refresh token issuance is configured for BDD tests

  @S01 @FR-014 @docker
  Scenario: Default public client teamboard is seeded after startup
    Given an empty PostgreSQL database is running
    And identity-service oauth client registry is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    When I send "GET" to "/health/ready"
    Then the status code is 200
    And the database contains registered oauth client "teamboard" with type "public"

  @S02 @FR-014 @BR-007 @docker
  Scenario: Confidential client secret is stored only as a hash
    Given an empty PostgreSQL database is running
    And identity-service oauth client registry is configured for BDD tests
    And identity-service has confidential oauth client "bdd-machine" with secret "MachineSecret-TestValue-12"
    And identity-service log capture is enabled
    And identity-service is configured to use that database and applies migrations on startup
    Then the database stores a hash for oauth client "bdd-machine" and not the plaintext secret "MachineSecret-TestValue-12"
    And captured logs contain no oauth client secret

  @S03 @FR-014 @BR-005
  Scenario: Redirect URI matching is exact
    When oauth redirect URI "https://app.example/callback" is checked against registered URIs:
      | uri |
      | https://app.example/callback |
    Then oauth redirect URI match result is true
    When oauth redirect URI "https://app.example/callback/" is checked against registered URIs:
      | uri |
      | https://app.example/callback |
    Then oauth redirect URI match result is false

  @S04 @FR-014
  Scenario: Unknown oauth client is not present in the database
    Given an empty PostgreSQL database is running
    And identity-service oauth client registry is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    Then the database does not contain registered oauth client "unknown-client-id"

  @S05 @BR-007 @docker
  Scenario: Startup sync logs do not echo client secrets
    Given an empty PostgreSQL database is running
    And identity-service oauth client registry is configured for BDD tests
    And identity-service has confidential oauth client "log-check" with secret "DoNotLogThisClientSecret99"
    And identity-service log capture is enabled
    And identity-service is configured to use that database and applies migrations on startup
    Then captured logs contain no oauth client secret