@TB-021 @identity-service
Feature: identity-service health
  As an operator
  I want identity-service to expose /health/live and /health/ready
  So that the orchestrator knows the service is alive and when it is ready to accept requests

  @S01 @NFR-001
  Scenario: Live returns 200 when the database is not running
    Given identity-service is configured to use a database that is not running
    When I send "GET" to "/health/live"
    Then the status code is 200

  @S08 @NFR-001
  Scenario: identity-service telemetry carries the correct service name
    Given identity-service is configured to use a database that is not running
    When I send "GET" to "/health/live"
    Then the service telemetry is named "identity-service"
