@TB-010 @gateway
Feature: Gateway health
  As an operator
  I want the gateway to expose /health/live and /health/ready independent of the upstream
  So that the orchestrator can check the gateway without inheriting failures of the services behind it

  @S03 @FR-002 @BR-003
  Scenario: Live returns 200
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When I send "GET" to "/health/live"
    Then the status code is 200
    And the fake upstream receives no request

  @S04 @FR-002
  Scenario: Ready returns 200 when the upstream has stopped
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    And the fake upstream has stopped
    When I send "GET" to "/health/ready"
    Then the status code is 200
