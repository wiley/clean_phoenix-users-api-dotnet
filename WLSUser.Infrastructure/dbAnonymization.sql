UPDATE Users SET 
	Users.Username = CONCAT(id,'@ck.com'),
	Users.Email = CONCAT(id,'@ck.com'),
	Users.RecoveryEmail = CONCAT(id,'-recovery@ck.com'),
	Users.FirstName = CONCAT('firstname', id),
	Users.LastName = CONCAT('lastname', id),
	Users.OrigPasswordSalt = '',
	Users.OrigPasswordHash = '',
	Users.StrongPasswordSalt = 'oQoN9gh/mqtZg0Aisxhlsw==',
	Users.StrongPasswordHash = 'r9cCJCdbfc9djoYbEaCoSZwygWSyhJy9frUt8zXHxcA=',
	Users.AvatarUrl = NULL,
	Users.City = 'ANONYMIZED',
	Users.PhoneNumber = 'ANONYMIZED',
	Users.PostalCode = 'ANONYMIZED'
;

UPDATE UserMappings SET 
	UserMappings.PlatformPasswordSalt = 'oQoN9gh/mqtZg0Aisxhlsw==',
	UserMappings.PlatformPasswordHash = 'r9cCJCdbfc9djoYbEaCoSZwygWSyhJy9frUt8zXHxcA=',
	UserMappings.PlatformPassowrdMethod = 'SHA256'
;