﻿#if ASPNET_INTEGRATION_TESTS
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace IbanNet.DataAnnotations
{
#if NETCOREAPP3_1
	[TestFixture(typeof(AspNet30WebHostFixture))]
#endif
#if NETCOREAPP2_2
	[TestFixture(typeof(AspNet22WebHostFixture))]
#endif
	public class AspNetIntegrationTest
	{
		private readonly WebHostFixture _fixture;

		public AspNetIntegrationTest(Type fixtureType)
		{
			_fixture = (WebHostFixture)Activator.CreateInstance(fixtureType);
		}

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			_fixture.Start();
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_fixture.Dispose();
		}

		[Test]
		public async Task Given_valid_iban_when_posting_with_attribute_validation_it_should_validate()
		{
			const string validIban = "NL91 ABNA 0417 1643 00";
			using HttpClient client = _fixture.TestServer.CreateClient();

			// Act
			HttpResponseMessage response = await client.SendAsync(CreateSaveRequest(validIban));

			// Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			(await response.Content.ReadAsStringAsync()).Should().Be($"\"{validIban}\"");
		}

		[Test]
		public async Task Given_invalid_iban_when_posting_with_attribute_validation_it_should_validate()
		{
			const string invalidIban = "invalid-iban";
			using HttpClient client = _fixture.TestServer.CreateClient();

			// Act
			HttpResponseMessage response = await client.SendAsync(CreateSaveRequest(invalidIban));

			// Assert
			response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
			string responseContent = await response.Content.ReadAsStringAsync();
			_fixture.MapToErrors(responseContent)
				.Should()
				.ContainKey("BankAccountNumber")
				.WhichValue.Should()
				.Contain("The field 'BankAccountNumber' is not a valid IBAN.");
		}

		private static HttpRequestMessage CreateSaveRequest(string iban)
		{
			return new HttpRequestMessage(HttpMethod.Post, "test/save")
			{
				Headers =
				{
					Accept =
					{
						new MediaTypeWithQualityHeaderValue("application/json")
					}
				},
				Content = new StringContent($"{{\"BankAccountNumber\":\"{iban}\"}}", Encoding.UTF8, "application/json")
			};
		}
	}
}
#endif
