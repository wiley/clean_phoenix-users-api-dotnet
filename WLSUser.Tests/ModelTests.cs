using FluentAssertions;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;

using Xunit;

namespace WLSUser.Tests
{
    public class ModelTests
    {
        private const string StringLength8 = "EightCh8";
        private const string StringLength30 = "This_Sentence_Is_30_Characters";
        private const string StringLength50 = "abc_def_ghi_jkl_mno_pqrs_tuv_wxyz@ABC_DEF_GHI_JKL!";
        private const string StringLength100 = "Sed_ut_perspiciatis_unde_omnis_iste_natus_error_sit_voluptatem_accusantium_doloremque_laudantium@_to";
        private const string StringLength200 = "Far_far_away@_behind_the_word_mountains@_far_from_the_countries_Vokalia_and_Consonantia@_there_live_the_blind_texts!_Separated_they_live_in_Bookmarksgrove_right_at_the_coast_of_the_Semantics@_a_large!";

        public ModelTests() { }

        #region LoginModel Tests

        [Fact]
        public void LoginModelReturnsInvalidMissingUsername()
        {
            //Arrange
            var model = new LoginRequestModel
            {
                Username = null, //missing username
                UserType = UserTypeEnum.Any,
                Password = "Does not matter"
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The Username field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void LogingModelReturnsInvalidBadUsername()
        {
            //Arrange
            var model = new LoginRequestModel
            {
                Username = "", //username size invalid
                UserType = UserTypeEnum.Any,
                Password = "Does not matter"
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The Username field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void LoginModelReturnsInvalidMissingPassword()
        {
            //Arrange
            var model = new LoginRequestModel
            {
                Username = "Some Username",
                UserType = UserTypeEnum.Any,
                Password = null
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The Password field is required.", result[0].ErrorMessage);
        }

        //NASA-234 - Username field should support 100 characters
        [Fact]
        public void LogingModelReturnsInvalidForUsernameOver100()
        {
            //Arrange
            var model = new LoginRequestModel
            {
                Username = new string('A', 101), //username size invalid
                UserType = UserTypeEnum.Any,
                Password = "Does not matter"
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field Username must be a string with a minimum length of 8 and a maximum length of 100.", result[0].ErrorMessage);
        }

        //NASA-234 - Username field should support 100 characters
        [Fact]
        public void LoginModelReturnsValidFor100CharacterUsername()
        {
            //Arrange
            var model = new LoginRequestModel
            {
                Username = new string('A', 100),
                UserType = UserTypeEnum.Any,
                Password = "Does not matter"
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.True(valid, "Model Validation failed on something new");
            Assert.True(result != null);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void LoginModelReturnsValidState()
        {
            //Arrange
            var model = new LoginRequestModel
            {
                Username = "Some Username",
                UserType = UserTypeEnum.Any,
                Password = "Does not matter"
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.True(valid, "Model Validation failed on something new");
            Assert.True(result != null);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region ForgotPasswordModel Tests

        [Fact]
        public void ForgotPasswordModelReturnsInvalidMissingUsername()
        {
            //Arrange
            var model = new ForgotPasswordRequestModel
            {
                Username = null,
                UserType = UserTypeEnum.Any
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The Username field is required.", result[0].ErrorMessage);
        }

        //NASA-234 - Username field should support 100 characters
        [Fact]
        public void ForgotPasswordModelReturnsInvalidForUsernameOver100()
        {
            //Arrange
            var model = new ForgotPasswordRequestModel
            {
                Username = new string('A', 101),
                UserType = UserTypeEnum.Any
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field Username must be a string with a minimum length of 8 and a maximum length of 100.", result[0].ErrorMessage);
        }

        //NASA-234 - Username field should support 100 characters
        [Fact]
        public void ForgotPasswordModelReturnsValidFor100CharacterUsername()
        {
            //Arrange
            var model = new ForgotPasswordRequestModel
            {
                Username = new string('A', 100),
                UserType = UserTypeEnum.Any
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.True(valid, "Model Validation failed on something new");
            Assert.True(result != null);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void ForgotPasswordModelReturnsValidState()
        {
            //Arrange
            var model = new ForgotPasswordRequestModel
            {
                Username = "Some Username",
                UserType = UserTypeEnum.Any
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.True(valid, "Model Validation failed on something new");
            Assert.True(result != null);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region AccessType Tests

        [Fact]
        public void AccessTypeReturnsInvalidMissingAccessTypeName()
        {
            var model = new AccessType
            {
                AccessTypeID = 1,
                AccessTypeName = null
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.NotNull(result);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The AccessTypeName field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void AccessTypeReturnsInvalidAccessTypeNameTooLarge()
        {
            var model = new AccessType
            {
                AccessTypeID = 1,
                AccessTypeName = new string('A', 246),
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.NotNull(result);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field AccessTypeName must be a string or array type with a maximum length of '245'.", result[0].ErrorMessage);
        }

        [Fact]
        public void AccessTypeReturnsValidAccessTypeNameMaxLength()
        {
            var model = new AccessType
            {
                AccessTypeID = 1,
                AccessTypeName = new string('A', 245),
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void AccessTypeReturnsValidState()
        {
            var model = new AccessType
            {
                AccessTypeID = 1,
                AccessTypeName = "Organization",
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region Brand Tests

        [Fact]
        public void BrandReturnsInvalidMissingBrandName()
        {
            var model = new Brand
            {
                BrandID = 1,
                BrandName = null
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.NotNull(result);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The BrandName field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void BrandReturnsInvalidBrandNameTooLarge()
        {
            var model = new Brand
            {
                BrandID = 1,
                BrandName = new string('A', 246)
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.NotNull(result);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field BrandName must be a string or array type with a maximum length of '245'.", result[0].ErrorMessage);
        }

        [Fact]
        public void BrandReturnsValidBrandNameMaxLength()
        {
            var model = new Brand
            {
                BrandID = 1,
                BrandName = new string('A', 245)
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void BrandReturnsValidState()
        {
            var model = new Brand
            {
                BrandID = 1,
                BrandName = "Organization"
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region RoleType Tests

        [Fact]
        public void RoleTypeReturnsInvalidMissingRoleTypeName()
        {
            var model = new RoleType
            {
                RoleTypeID = 1,
                BrandID = 1,
                RoleName = null
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.NotNull(result);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The RoleName field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void RoleTypeReturnsInvalidRoleTypeTooLarge()
        {
            var model = new RoleType
            {
                RoleTypeID = 1,
                BrandID = 1,
                RoleName = new string('A', 246)
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.NotNull(result);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field RoleName must be a string or array type with a maximum length of '245'.", result[0].ErrorMessage);
        }

        [Fact]
        public void RoleTypeReturnsValidRoleNameMaxLength()
        {
            var model = new RoleType
            {
                RoleTypeID = 1,
                BrandID = 1,
                RoleName = new string('A', 245)
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void RoleTypeReturnsValidState()
        {
            var model = new RoleType
            {
                RoleTypeID = 1,
                BrandID = 1,
                RoleName = "Catalyst Learner"
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region UserRole Tests

        [Fact]
        public void UserRoleReturnsValidState()
        {
            var model = new UserRole
            {
                UserRoleID = 1,
                UserID = 1,
                RoleTypeID = 1,
                Created = DateTime.Now
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region UserRoleAccess Tests

        [Fact]
        public void UserRoleAccessReturnsValidState()
        {
            var model = new UserRoleAccess
            {
                UserRoleID = 1,
                AccessTypeID = 1,
                AccessRefID = 1,
                GrantedBy = 1,
                Created = DateTime.Now
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region AddUserRoleRequest Tests

        [Fact]
        public void AddUserRoleRequestUniqueIDMinLengthReturnsValid()
        {
            var model = new AddUserRoleRequest
            {
                UniqueID = new string('A', 15),
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void AddUserRoleRequestUniqueIDMaxLengthReturnsValid()
        {
            var model = new AddUserRoleRequest
            {
                UniqueID = new string('A', 255),
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void AddUserRoleRequestUniqueIDTooShortReturnsInvalid()
        {
            var model = new AddUserRoleRequest
            {
                UniqueID = new string('A', 7),
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field UniqueID must be a string with a minimum length of 15 and a maximum length of 255.", result[0].ErrorMessage);
        }

        [Fact]
        public void AddUserRoleRequestUniqueIDTooLongReturnsInvalid()
        {
            var model = new AddUserRoleRequest
            {
                UniqueID = new string('A', 256),
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field UniqueID must be a string with a minimum length of 15 and a maximum length of 255.", result[0].ErrorMessage);
        }

        [Fact]
        public void AddUserRoleRequestReturnsValidState()
        {
            var model = new UserRoleRequest
            {
                UserID = 1,
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region DeleteUserRoleRequest Tests

        [Fact]
        public void DeleteUserRoleRequestUniqueIDMinLengthReturnsValid()
        {
            var model = new DeleteUserRoleRequest
            {
                UniqueID = new string('A', 15),
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void DeleteUserRoleRequestUniqueIDMaxLengthReturnsValid()
        {
            var model = new DeleteUserRoleRequest
            {
                UniqueID = new string('A', 255),
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void DeleteUserRoleRequestUniqueIDTooShortReturnsInvalid()
        {
            var model = new DeleteUserRoleRequest
            {
                UniqueID = new string('A', 7),
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field UniqueID must be a string with a minimum length of 15 and a maximum length of 255.", result[0].ErrorMessage);
        }

        [Fact]
        public void DeleteUserRoleRequestUniqueIDTooLongReturnsInvalid()
        {
            var model = new DeleteUserRoleRequest
            {
                UniqueID = new string('A', 256),
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field UniqueID must be a string with a minimum length of 15 and a maximum length of 255.", result[0].ErrorMessage);
        }

        [Fact]
        public void DeleteUserRoleRequestReturnsValidState()
        {
            var model = new UserRoleRequest
            {
                UserID = 1,
                RoleTypeID = 1
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region AuthRequest

        private const string AuthUsernameBase = "test@test.test";
        private const string AuthPasswordBase = "testPW123";

        public static List<object[]> AuthRequestEdgeCasesGood()
        {
            return new List<object[]>
            {
                //user_min
                new object[]
                {
                    new AuthRequest
                    {
                        Username = StringLength8,
                        Password = AuthPasswordBase
                    }
                },
                //password_min
                new object[]
                {
                    new AuthRequest
                    {
                        Username = AuthUsernameBase,
                        Password = StringLength8
                    }
                },
                //user_max
                new object[]
                {
                    new AuthRequest
                    {
                        Username = StringLength50,
                        Password = AuthPasswordBase
                    }
                },
                //password_max
                new object[]
                {
                    new AuthRequest
                    {
                        Username = AuthUsernameBase,
                        Password = StringLength50
                    }
                }
            };
        }

        public static List<object[]> AuthRequestEdgeCasesUserNameBad()
        {
            return new List<object[]>
            {
                //user_short
                new object[]
                {
                    new AuthRequest
                    {
                        Username = StringLength8.Substring(0,7),
                        Password = AuthPasswordBase
                    }
                },
                //user_long
                new object[]
                {
                    new AuthRequest
                    {
                        Username = StringLength100 + 'x',
                        Password = AuthPasswordBase
                    }
                }
            };
        }

        public static List<object[]> AuthRequestEdgeCasesPasswordBad()
        {
            return new List<object[]>
            {
                //password_short
                new object[]
                {
                    new AuthRequest
                    {
                        Username = AuthUsernameBase,
                        Password = StringLength8.Substring(0,7),
                    }
                },
                //password_long
                new object[]
                {
                    new AuthRequest
                    {
                        Username = AuthUsernameBase,
                        Password =  StringLength50 + 'x'
                    }
                }
            };
        }
        /// </summary>
        [Fact]
        public void AuthRequest_Valid()
        {
            var ar = new AuthRequest
            {
                Username = "test@test.test",
                Password = "abcJKL123"

            };
            var context = new ValidationContext(ar);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(ar, context, results, true);

            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Theory]
        [MemberData(nameof(AuthRequestEdgeCasesGood))]
        public void AuthRequest_Valid_StringLengthEdgeCasesGood(AuthRequest ar)
        {
            var context = new ValidationContext(ar);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(ar, context, results, true);

            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Theory]
        [MemberData(nameof(AuthRequestEdgeCasesUserNameBad))]
        public void AuthRequestUsername_Valid_StringLengthEdgeCasesBad(AuthRequest ar)
        {
            var context = new ValidationContext(ar);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(ar, context, results, true);

            Assert.False(isValid);
            results.Should().HaveCount(1);
            var result = results.First();
            Assert.Equal("The field Username must be a string with a minimum length of 8 and a maximum length of 100.", result.ErrorMessage);
            result.MemberNames.Should().HaveCount(1);
            Assert.Equal("Username", result.MemberNames.First());
        }

        [Theory]
        [MemberData(nameof(AuthRequestEdgeCasesPasswordBad))]
        public void AuthRequestPassword_Valid_StringLengthEdgeCasesBad(AuthRequest ar)
        {
            var context = new ValidationContext(ar);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(ar, context, results, true);

            Assert.False(isValid);
            results.Should().HaveCount(1);
            var result = results.First();
            Assert.Equal("The field Password is invalid.", result.ErrorMessage);
            result.MemberNames.Should().HaveCount(1);
            Assert.Equal("Password", result.MemberNames.First());
        }
        #endregion

        #region AuthResponse
        [Fact]
        public void AuthResponse_Valid()
        {
            var ar = new AuthResponse
            {
                AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJlcGljOnNpbmdsZXRvbjpsZWFybmVyOjcwMTYiLCJqdGkiOiI3YWRlYjdhMy05ZTIzLTQ4ZjUtOGYxNC0yMzQyN2NiYWRkOTEiLCJpYXQiOjE1NjUyNzIwNTUsInJvbCI6ImFwaV9hY2Nlc3MiLCJpZCI6ImVwaWM6c2luZ2xldG9uOmxlYXJuZXI6NzAxNiIsIm5iZiI6MTU2NTI3MjA1NSwiZXhwIjoxNTY1MzI2MDU1LCJpc3MiOiJ3ZWJBcGkiLCJhdWQiOiJodHRwczovL2xvY2FsaG9zdDo0NDMwNC8ifQ.C9V_mEQWivTXL_6ZlwsyIumi9ucPsPiXkXbU33Ciksg",
                Expires = new DateTime(2017, 1, 31),
                RefreshToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJlcGljOnNpbmdsZXRvbjpsZWFybmVyOjcwMTYiLCJqdGkiOiI3YWRlYjdhMy05ZTIzLTQ4ZjUtOGYxNC0yMzQyN2NiYWRkOTEiLCJpYXQiOjE1NjUyNzIwNTUsInJvbCI6ImFwaV9hY2Nlc3MiLCJpZCI6ImVwaWM6c2luZ2xldG9uOmxlYXJuZXI6NzAxNiIsIm5iZiI6MTU2NTI3MjA1NSwiZXhwIjoxNTY1MzI2MDU1LCJpc3MiOiJ3ZWJBcGkiLCJhdWQiOiJodHRwczovL2xvY2FsaG9zdDo0NDMwNC8ifQ.C9V_mEQWivTXL_6ZlwsyIumi9ucPsPiXkXbU33Ciksg",
                RefreshExpires = new DateTime(2018, 1, 31)
            };
            var context = new ValidationContext(ar);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(ar, context, results, true);

            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Fact]
        public void AuthResponse_InValid()
        {
            var ar = new AuthResponse
            {
                AccessToken = "",
                Expires = new DateTime(2017, 1, 31),
                RefreshToken = "",
                RefreshExpires = new DateTime(2018, 1, 31)
            };
            var context = new ValidationContext(ar);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(ar, context, results, true);

            Assert.False(isValid);
            results.Should().HaveCount(1);
            var result = results.First();
            Assert.Equal("The AccessToken field is required.", result.ErrorMessage);
            result.MemberNames.Should().HaveCount(1);
            Assert.Equal("AccessToken", result.MemberNames.First());
        }

        #endregion

        #region AuthLoginRespone

        [Fact]
        public void AuthLoginResponse_Valid()
        {
            var loginResponse = new LoginResponse
            {
                FirstName = "firstname",
                LastName = "lastname",
                Status = "Connected",
                UserName = "firstname.lastname@test.com",
                UserType = UserTypeEnum.Any,
                UniqueID = "1234"
            }; 
             
            var context = new ValidationContext(loginResponse);
            var failures = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(loginResponse, context, failures, true);
            
            Assert.True(isValid);
            Assert.Empty(failures);
        }

        #endregion

        #region JWTOptions

        [Fact]
        public void JwtOptions_Validation()
        {
            var ar = new JwtIssuerOptions { };
            var context = new ValidationContext(ar);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(ar, context, results, true);

            Assert.True(isValid);
            Assert.Empty(results);

            Assert.True(Math.Abs(TimeZoneInfo.ConvertTimeToUtc(DateTime.Now, TimeZoneInfo.Local).Ticks - ar.NotBefore.Ticks) <= new TimeSpan(0, 1, 0).Ticks);
            Assert.True(Math.Abs(TimeZoneInfo.ConvertTimeToUtc(DateTime.Now, TimeZoneInfo.Local).Ticks - ar.Expiration.Ticks) <= new TimeSpan(2, 1, 0).Ticks);
            Assert.True(Math.Abs(TimeZoneInfo.ConvertTimeToUtc(DateTime.Now, TimeZoneInfo.Local).Ticks - ar.IssuedAt.Ticks) <= new TimeSpan(0, 1, 0).Ticks);
        }
        [Fact]
        public void JwtOptions_ChangeValidForTime()
        {
            var ar = new JwtIssuerOptions
            {
                ValidFor = new TimeSpan(0, 5, 0)
            };
            var context = new ValidationContext(ar);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(ar, context, results, true);

            Assert.True(isValid);
            Assert.Empty(results);

            Assert.True(Math.Abs(TimeZoneInfo.ConvertTimeToUtc(DateTime.Now, TimeZoneInfo.Local).Ticks - ar.Expiration.Ticks) <= new TimeSpan(0, 10, 0).Ticks);
        }
        #endregion

        #region UserUniqueID

        [Fact]
        public void UserUniqueID_Valid()
        {
            var model = new UserUniqueID("epic:singleton:learner:1234");

            var context = new ValidationContext(model);
            var failures = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(model, context, failures, true);

            Assert.True(isValid);
            Assert.Empty(failures);
        }

        [Fact()]
        public void UserUniqueID_ToStringTest()
        {
            var uniqueID = "epic:singleton:learner:1234";
            var userUniqueID = new UserUniqueID(uniqueID);
            Assert.Equal(uniqueID, userUniqueID.ToString());
        }

        [Fact()]
        public void UserUniqueID_PlatformNameTest()
        {
            var uniqueID = "EPIC:singleton:learner:1234";
            var userUniqueID = new UserUniqueID(uniqueID);
            Assert.Equal("epic", userUniqueID.PlatformName);
        }

        [Fact()]
        public void UserUniqueID_PlatformUserIDTest()
        {
            var uniqueID = "EPIC:singleton:learner:1234";
            var userUniqueID = new UserUniqueID(uniqueID);
            Assert.Equal("1234", userUniqueID.PlatformUserID);
        }

        [Fact()]
        public void UserUniqueID_PlatformPasswordMethodTest()
        {
            var uniqueID = "EPIC:singleton:learner:1234";
            var userUniqueID = new UserUniqueID(uniqueID);
            Assert.Equal("SHA1", userUniqueID.PlatformPasswordMethod());
        }

        #endregion

        #region RoleAccessReference Tests

        [Fact]
        public void RoleAccessReference_Valid()
        {
            var model = new RoleAccessReference
            {
                RoleType = new RoleType { RoleTypeID = 1, BrandID = 1, RoleName = "Role Type Test" },
                AccessType = new AccessType { AccessTypeID = 1, AccessTypeName = "Access Type Test" },
                UserRoleAccessList = new List<UserRoleAccess> { new UserRoleAccess { UserRoleID = 1, AccessTypeID = 1, AccessRefID = 1234, GrantedBy = 1, Created = DateTime.Parse("1/1/2020") } }
            };

            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(model, context, result, true);

            Assert.True(valid, "Model Validation failed on something new");
            Assert.NotNull(result);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion

        #region LoginSsoRequestModel Tests

        [Fact]
        public void LoginSsoRequestModelReturnsInvalidMissingFederationName()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = null, //missing federationname
                Code = "Does not matter",
                State = Guid.NewGuid().ToString()
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The FederationName field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void LoginSsoRequestModelReturnsInvalidBadFederationName()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = "", //too few characters
                Code = "Does not matter",
                State = Guid.NewGuid().ToString()
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The FederationName field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void LoginSsoRequestModelReturnsInvalidForFederationNameOver50()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = new string('A', 51), //federationname size invalid
                Code = "Does not matter",
                State = Guid.NewGuid().ToString()
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field FederationName must be a string with a minimum length of 3 and a maximum length of 50.", result[0].ErrorMessage);
        }

        [Fact]
        public void LoginSsoRequestModelReturnsValidFor50CharacterFederationName()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = new string('A', 50),
                Code = "Does not matter",
                State = Guid.NewGuid().ToString()
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.True(valid, "Model Validation failed on something new");
            Assert.True(result != null);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        [Fact]
        public void LoginSsoRequestModelReturnsInvalidMissingCode()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = "wiley",
                Code = null,
                State = Guid.NewGuid().ToString()
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The Code field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void LoginSsoRequestModelReturnsInvalidBadCode()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = "wiley",
                Code = "", //too few characters
                State = Guid.NewGuid().ToString()
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The Code field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void LoginSsoRequestModelReturnsInvalidMissingState()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = "wiley",
                Code = "Does not matter",
                State = null
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The State field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void LoginSsoRequestModelReturnsInvalidBadState()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = "wiley",
                Code = "Does not matter",
                State = "" //too few characters
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The State field is required.", result[0].ErrorMessage);
        }

        [Fact]
        public void LoginSsoRequestModelReturnsInvalidForStateOver100()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = "wiley",
                Code = "Does not matter",
                State = new string('A', 101) //state size invalid
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.False(valid, "Model Validation did not validate invalid state");
            Assert.True(result != null);
            Assert.False(result.Count < 1, "Model Validation did not list invalid state");
            Assert.False(result.Count > 1, "Additional Model Validation errors occurred - test is no longer testing what was intented");
            Assert.Equal("The field State must be a string with a minimum length of 36 and a maximum length of 100.", result[0].ErrorMessage);
        }

        [Fact]
        public void LoginSsoRequestModelReturnsValidFor100CharacterState()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = "wiley",
                Code = "Does not matter",
                State = new string('A', 100)
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.True(valid, "Model Validation failed on something new");
            Assert.True(result != null);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }


        [Fact]
        public void LoginSsoRequestModelReturnsValid()
        {
            //Arrange
            var model = new LoginSsoRequestModel
            {
                FederationName = "wiley",
                Code = "code",
                State = Guid.NewGuid().ToString()
            };
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(model, context, result, true);

            //Assert
            Assert.True(valid, "Model Validation failed on something new");
            Assert.True(result != null);
            Assert.True(result.Count == 0, "Model Validation failed on something new");
        }

        #endregion
    }
}