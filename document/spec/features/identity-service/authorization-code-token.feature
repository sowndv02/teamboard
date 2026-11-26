@TB-043 @identity-service
Feature: identity-service OAuth authorization code token exchange
  As an OAuth client
  I want to exchange an authorization code for a user access token
  So that users can complete the authorization code grant with PKCE

  Background:
    Given identity-service rate limiting is disabled
    And identity-service oauth client registry is configured for BDD tests
    And identity-service authorization code grant is enabled for BDD tests
    And identity-service client credentials grant is enabled for BDD tests
    And identity-service access token issuance is configured for BDD tests
    And identity-service client "teamboard" has redirect uri "http://localhost:4200/callback" and allowed scopes:
      | scope  |
      | openid |

  @S01 @FR-013 @NFR-007 @docker
  Scenario: Public client exchanges an authorization code with a valid PKCE verifier
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service metrics are being observed for authorization code exchanges
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s01@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-code-s01" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s01@example.com" with password "ValidPass123456"
    When I exchange the stored authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" without client_secret
    Then the status code is 200
    And the token response includes a Bearer access token with expires_in
    And the oauth token access token is a user token for email "code-s01@example.com" with audience "teamboard"
    And at least one authorization code exchange metric was recorded

  @S01b @FR-013 @docker
  Scenario: Public client exchanges an authorization code without sending client_secret
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s01b@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-code-s01b" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s01b@example.com" with password "ValidPass123456"
    When I exchange the stored authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" without client_secret
    Then the status code is 200
    And the token response includes a Bearer access token with expires_in
    And the oauth token access token is a user token for email "code-s01b@example.com" with audience "teamboard"

  @S02 @FR-013 @BR-005 @docker
  Scenario: Wrong PKCE verifier is rejected and the code stays unused
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s02@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-code-s02" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s02@example.com" with password "ValidPass123456"
    When I exchange the stored authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" and code_verifier "wrong-verifier-that-does-not-match-pkce-challenge-value" without client_secret
    Then the status code is 400
    And the oauth token response error is "invalid_grant"
    And the token response has no access_token
    And the stored authorization code is not marked as used in the database

  @S03 @FR-013 @BR-005 @docker
  Scenario: Reusing the same authorization code is rejected
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s03@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-code-s03" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s03@example.com" with password "ValidPass123456"
    When I exchange the stored authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" without client_secret
    Then the status code is 200
    And the token response includes a Bearer access token with expires_in
    When I exchange the stored authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" without client_secret
    Then the status code is 400
    And the oauth token response error is "invalid_grant"
    And the token response has no access_token

  @S04 @FR-013 @BR-005 @docker
  Scenario: Expired authorization code is rejected
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s04@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-code-s04" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s04@example.com" with password "ValidPass123456"
    And the stored authorization code is expired in the database
    When I exchange the stored authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" without client_secret
    Then the status code is 400
    And the oauth token response error is "invalid_grant"
    And the token response has no access_token

  @S05 @FR-013 @BR-005 @docker
  Scenario: Redirect URI mismatch is rejected
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s05@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-code-s05" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s05@example.com" with password "ValidPass123456"
    When I exchange the stored authorization code for client "teamboard" with redirect_uri "http://localhost:4200/other-callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" without client_secret
    Then the status code is 400
    And the oauth token response error is "invalid_grant"
    And the token response has no access_token
    And the stored authorization code is not marked as used in the database

  @S06 @FR-013 @FR-012 @docker
  Scenario: Client ID mismatch is rejected
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s06@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-code-s06" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s06@example.com" with password "ValidPass123456"
    When I exchange the stored authorization code for client "other-bdd-client" with redirect_uri "http://localhost:4200/callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" without client_secret
    Then the status code is 400
    And the oauth token response error is "invalid_grant"
    And the token response has no access_token
    And the stored authorization code is not marked as used in the database

  @S07 @FR-014 @docker
  Scenario: Confidential client without a valid client_secret is rejected
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service has confidential oauth client "code-confidential" with secret "bdd-code-exchange-secret-32chars"
    And identity-service client "code-confidential" has redirect uri "http://localhost:4200/callback" and allowed scopes:
      | scope  |
      | openid |
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s07@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "code-confidential" with redirect_uri "http://localhost:4200/callback" state "state-code-s07" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s07@example.com" with password "ValidPass123456"
    When I exchange the stored authorization code for client "code-confidential" with redirect_uri "http://localhost:4200/callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" and client_secret "wrong-confidential-secret-value-99"
    Then the status code is 401
    And the oauth token response error is "invalid_client"
    And the token response has no access_token
    And the stored authorization code is not marked as used in the database

  @S07b @FR-014 @docker
  Scenario: Confidential client must send client_secret at token exchange
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service has confidential oauth client "code-confidential-missing" with secret "bdd-code-missing-secret-32charxx"
    And identity-service client "code-confidential-missing" has redirect uri "http://localhost:4200/callback" and allowed scopes:
      | scope  |
      | openid |
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s07b@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "code-confidential-missing" with redirect_uri "http://localhost:4200/callback" state "state-code-s07b" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s07b@example.com" with password "ValidPass123456"
    When I exchange the stored authorization code for client "code-confidential-missing" with redirect_uri "http://localhost:4200/callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" without client_secret
    Then the status code is 401
    And the oauth token response error is "invalid_client"
    And the token response has no access_token
    And the stored authorization code is not marked as used in the database

  @S08 @BR-007 @docker
  Scenario: Authorization code exchange does not log the code or code_verifier
    Given an empty PostgreSQL database is running
    And identity-service http client preserves cookies between requests
    And identity-service log capture is enabled
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "code-s08@example.com" and password "ValidPass123456"
    And I have obtained an authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" state "state-code-s08" scope "openid" pkce challenge "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" method "S256" as user "code-s08@example.com" with password "ValidPass123456"
    And identity-service log capture ignores entries before the token exchange
    When I exchange the stored authorization code for client "teamboard" with redirect_uri "http://localhost:4200/callback" and code_verifier "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" without client_secret
    Then the status code is 200
    And the token response includes a Bearer access token with expires_in
    And captured logs contain no authorization code or code_verifier secrets
    And the response body does not contain the stored authorization code plaintext
    And the response body does not contain "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"