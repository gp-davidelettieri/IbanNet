﻿#if NET5_0_OR_GREATER
using System.Text;
using System.Text.Json;
using IbanNet.Registry;
using TestHelpers;

namespace IbanNet.Json
{
    [Collection(nameof(SetsStaticValidator))]
    public class IbanJsonConverterTests
    {
        [Fact]
        public void When_creating_default_instance_it_should_use_static_validator()
        {
            const string ibanStr = "NL91ABNA0417164300";
            var validatorStub = new IbanValidatorStub();

            ReadOnlySpan<byte> buffer = Encoding.UTF8.GetBytes($"\"{ibanStr}\"");
            var reader = new Utf8JsonReader(buffer);
            reader.Read();

            Iban.Validator = validatorStub;
            try
            {
                var sut = new IbanJsonConverter();

                // Act
                sut.Read(ref reader, typeof(Iban), new JsonSerializerOptions());

                // Assert
                validatorStub.Verify(m => m.Validate(ibanStr), Times.Once);
            }
            finally
            {
                Iban.Validator = null!;
            }
        }

        [Fact]
        public void Given_that_parser_is_used_when_creating_instance_it_should_use_parser()
        {
            const string ibanStr = "NL91ABNA0417164300";

            Iban outIban = null;
            var parserMock = new Mock<IIbanParser>();
            parserMock
                .Setup(m => m.TryParse(It.IsAny<string>(), out outIban))
                .Returns(true)
                .Verifiable();

            ReadOnlySpan<byte> buffer = Encoding.UTF8.GetBytes($"\"{ibanStr}\"");
            var reader = new Utf8JsonReader(buffer);
            reader.Read();

            var sut = new IbanJsonConverter(parserMock.Object);

            // Act
            sut.Read(ref reader, typeof(Iban), new JsonSerializerOptions());

            // Assert
            parserMock.Verify();
        }

        [Fact]
        public void Given_that_a_complex_record_has_an_iban_when_deserializing_it_should_equal_expected()
        {
            var parser = new IbanParser(IbanRegistry.Default);

            var payment1 = new Payment(parser.Parse("NL91 ABNA 0417 1643 00"), 100M);
            const string expectedJson = "{\"BankAccountNumber\":\"NL91ABNA0417164300\",\"Amount\":100}";

            // Act
            string json = JsonSerializer.Serialize(payment1);
            Payment payment2 = JsonSerializer.Deserialize<Payment>(json);

            // Assert
            json.Should().Be(expectedJson);
            payment2.Should().Be(payment1);
        }

        public record Payment(Iban BankAccountNumber, decimal Amount) { }
    }
}
#endif