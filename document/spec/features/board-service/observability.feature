@TB-001 @board-service
Feature: board-service traces appear in Jaeger
  As a developer
  I want traces of requests to board-service to appear in Jaeger
  So that I can debug across multiple services

  @S11 @FR-003 @NFR-005 @docker
  Scenario: A request to board-service appears as a trace in Jaeger
    Given Docker is available
    And the main compose and observability compose are running under a dedicated project name
    When I call "GET /health/live" on host port 6060
    Then Jaeger has a trace for service "board-service" within the wait time
    And Jaeger and the OTLP port bind only to 127.0.0.1
