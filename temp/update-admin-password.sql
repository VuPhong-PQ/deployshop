-- Update password cho user admin thanh "vuphong"
-- BCrypt hash: $2a$11$TV0P46IzB1/KjCczopEKOOLInbcmUG2MlnwZC9m0UQbCf5qgvrKmO

UPDATE Staffs 
SET PasswordHash = '$2a$11$TV0P46IzB1/KjCczopEKOOLInbcmUG2MlnwZC9m0UQbCf5qgvrKmO' 
WHERE Username = 'admin';

SELECT StaffId, Username, PasswordHash FROM Staffs WHERE Username = 'admin';
