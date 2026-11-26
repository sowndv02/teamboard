@TB-034 @identity-service
Feature: identity-service JWKS publication
  As a resource server
  I want to fetch public signing keys from a well-known JWKS URL
  So that I can validate access tokens without calling identity per request

  Background:
    Given identity-service rate limiting is disabled
    And identity-service JWKS signing keys are configured for BDD tests

  @S01 @FR-007
  Scenario: JWKS returns the active public key with kid
    Given identity-service is running for JWKS without a database
    When I send GET to "/.well-known/jwks.json"
    Then the status code is 200
    And the JWKS document includes public key with kid "test-active-1"
    And the JWKS response body does not contain private key material

  @S02 @FR-007
  Scenario: JWKS response is cacheable for ten minutes
    Given identity-service is running for JWKS without a database
    When I send GET to "/.well-known/jwks.json"
    Then the status code is 200
    And the JWKS response has cache control public max-age 600

  @S03 @FR-007
  Scenario: JWKS is unavailable when no signing key is active
    Given identity-service is configured without signing keys
    And identity-service is running for JWKS without a database
    When I send GET to "/.well-known/jwks.json"
    Then the status code is 503

  @S04 @NFR-007
  Scenario: Successful JWKS response records served metric
    Given identity-service metrics are being observed for JWKS served
    And identity-service is running for JWKS without a database
    When I send GET to "/.well-known/jwks.json"
    Then the status code is 200
    And the identity counter "jwks_served_total" increased by at least 1