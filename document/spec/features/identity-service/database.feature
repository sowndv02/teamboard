@TB-021 @identity-service @docker
Feature: identity-service database
  As an operator
  I want identity-service to report ready only when the database is usable and to recover through migrations
  So that it accepts no logins when there is nowhere to store them and the dev environment can be rebuilt from scratch

  @S02 @NFR-001
  Scenario: Ready returns 200 when the database is running
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I send "GET" to "/health/ready"
    Then the status code is 200

  @S03 @NFR-001 @NFR-003
  Scenario: Ready returns 503 when the database has stopped
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    And the database has stopped
    When I send "GET" to "/health/ready"
    Then the status code is 503
    And the response body contains no password or connection string

  @S04 @NFR-009
  Scenario: The baseline migration applies to an empty database and a second run changes nothing
    Given an empty PostgreSQL database is running
    When identity-service starts twice in a row against that database and applies migrations
    Then the database migration history has exactly one baseline record

  @S07 @NFR-009
  Scenario: Migrations are not applied when the auto-apply flag is off
    Given an empty PostgreSQL database is running
    When identity-service starts against that database without enabling auto-apply of migrations
    Then identity-service still returns status code 200 for the liveness check
    And the database has no migration history
