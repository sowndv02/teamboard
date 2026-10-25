@TB-010 @gateway
Feature: Gateway runs in compose
  As a developer
  I want to run the gateway with docker compose
  So that I can try the system entry point locally

  @S05 @FR-002 @NFR-005 @docker
  Scenario: Compose starts the gateway alone and it is ready within 10 seconds
    Given Docker is available
    And the gateway image has been built with the main compose file
    When I start only the gateway service with compose under a dedicated project name
    Then host port 8080 returns 200 for "/health/live"
    And "/health/ready" returns 200 within 10 seconds of the start command
