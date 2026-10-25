@TB-001 @shared
Feature: ServiceDefaults OpenTelemetry telemetry
  As a developer
  I want traces, metrics and logs exported through OpenTelemetry
  So that I can debug several services without leaking sensitive data

  @S08 @FR-003
  Scenario: A request creates a span named after the board-service service
    Given a host using AddServiceDefaults with service name "board-service" that has started
    When I call "GET /health/live"
    Then there is a request span with service.name "board-service"

  @S09 @FR-003
  Scenario: The service still starts when the OTLP endpoint is missing
    Given the OTLP endpoint environment variable is not set
    And a host using AddServiceDefaults with service name "board-service" that has started
    When I call "GET /health/live"
    Then the status code is 200

  @S10 @NFR-004
  Scenario: Telemetry contains neither Authorization nor body
    Given a host using AddServiceDefaults with service name "board-service" that has started
    When I send a write request with Authorization header "Bearer secret-token-123", cookie "session=secret-cookie-789", query "code=secret-query-321" and body "secret-content-456"
    Then telemetry has recorded at least one span
    And no span or log record contains "secret-token-123"
    And no span or log record contains "secret-cookie-789"
    And no span or log record contains "secret-query-321"
    And no span or log record contains "secret-content-456"

  @S12 @FR-003
  Scenario: A request creates a web server metric
    Given a host using AddServiceDefaults with service name "board-service" that has started
    When I call "GET /health/live"
    Then telemetry has recorded at least one metric

  @S13 @FR-003
  Scenario: A request creates a log record
    Given a host using AddServiceDefaults with service name "board-service" that has started
    When I call "GET /health/live"
    Then telemetry has recorded at least one log record

  @S16 @FR-003
  Scenario: The service keeps running when the OTLP endpoint is unreachable
    Given a host using AddServiceDefaults with service name "board-service" and an OTLP endpoint with no listener that has started
    When I call "GET /health/live"
    Then the status code is 200
