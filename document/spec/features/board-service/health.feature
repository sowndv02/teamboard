@TB-001 @board-service
Feature: board-service health
  As an operator
  I want board-service to expose /health/live and /health/ready
  So that the orchestrator can check the service

  @S04 @FR-002
  Scenario: Live returns 200 when the service has started
    Given board-service is running
    When I call "GET /health/live"
    Then the status code is 200

  @S05 @FR-002
  Scenario: Ready returns 200 when the service is ready
    Given board-service is running
    When I call "GET /health/ready"
    Then the status code is 200
