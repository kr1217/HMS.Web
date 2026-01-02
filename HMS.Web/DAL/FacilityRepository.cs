using HMS.Web.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HMS.Web.DAL
{
    public class FacilityRepository
    {
        private readonly DatabaseHelper _db;

        public FacilityRepository(DatabaseHelper db)
        {
            _db = db;
        }

        // --- Wards ---
        public List<Ward> GetWards()
        {
            return _db.ExecuteQuery("SELECT * FROM Wards WHERE IsActive = 1", r => new Ward
            {
                WardId = (int)r["WardId"],
                WardName = r["WardName"].ToString()!,
                Floor = r["Floor"]?.ToString() ?? "",
                Wing = r["Wing"]?.ToString() ?? "",
                IsActive = (bool)r["IsActive"]
            });
        }

        public void SaveWard(Ward ward)
        {
            if (ward.WardId == 0)
            {
                string sql = "INSERT INTO Wards (WardName, Floor, Wing, IsActive) VALUES (@WardName, @Floor, @Wing, @IsActive)";
                _db.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@WardName", ward.WardName),
                    new SqlParameter("@Floor", (object?)ward.Floor ?? DBNull.Value),
                    new SqlParameter("@Wing", (object?)ward.Wing ?? DBNull.Value),
                    new SqlParameter("@IsActive", ward.IsActive)
                });
            }
            else
            {
                string sql = "UPDATE Wards SET WardName=@WardName, Floor=@Floor, Wing=@Wing, IsActive=@IsActive WHERE WardId=@WardId";
                _db.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@WardId", ward.WardId),
                    new SqlParameter("@WardName", ward.WardName),
                    new SqlParameter("@Floor", (object?)ward.Floor ?? DBNull.Value),
                    new SqlParameter("@Wing", (object?)ward.Wing ?? DBNull.Value),
                    new SqlParameter("@IsActive", ward.IsActive)
                });
            }
        }

        // --- Room Types ---
        public List<RoomType> GetRoomTypes()
        {
            return _db.ExecuteQuery("SELECT * FROM RoomTypes", r => new RoomType
            {
                RoomTypeId = (int)r["RoomTypeId"],
                TypeName = r["TypeName"].ToString()!,
                DailyRate = (decimal)r["DailyRate"],
                Description = r["Description"]?.ToString() ?? ""
            });
        }

        public void SaveRoomType(RoomType type)
        {
            if (type.RoomTypeId == 0)
            {
                string sql = "INSERT INTO RoomTypes (TypeName, DailyRate, Description) VALUES (@TypeName, @DailyRate, @Description)";
                _db.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@TypeName", type.TypeName),
                    new SqlParameter("@DailyRate", type.DailyRate),
                    new SqlParameter("@Description", (object?)type.Description ?? DBNull.Value)
                });
            }
            else
            {
                string sql = "UPDATE RoomTypes SET TypeName=@TypeName, DailyRate=@DailyRate, Description=@Description WHERE RoomTypeId=@RoomTypeId";
                _db.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@RoomTypeId", type.RoomTypeId),
                    new SqlParameter("@TypeName", type.TypeName),
                    new SqlParameter("@DailyRate", type.DailyRate),
                    new SqlParameter("@Description", (object?)type.Description ?? DBNull.Value)
                });
            }
        }

        // --- Rooms ---
        public List<Room> GetRooms()
        {
            string sql = @"
                SELECT r.*, w.WardName, rt.TypeName as RoomTypeName 
                FROM Rooms r
                LEFT JOIN Wards w ON r.WardId = w.WardId
                LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
                WHERE r.IsActive = 1";

            return _db.ExecuteQuery(sql, r => new Room
            {
                RoomId = (int)r["RoomId"],
                WardId = r["WardId"] != DBNull.Value ? (int)r["WardId"] : 0,
                RoomNumber = r["RoomNumber"].ToString()!,
                RoomTypeId = r["RoomTypeId"] != DBNull.Value ? (int)r["RoomTypeId"] : 0,
                IsActive = (bool)r["IsActive"],
                WardName = r["WardName"]?.ToString(),
                RoomTypeName = r["RoomTypeName"]?.ToString()
            });
        }

        public void SaveRoom(Room room)
        {
            if (room.RoomId == 0)
            {
                string sql = "INSERT INTO Rooms (WardId, RoomNumber, RoomTypeId, IsActive) VALUES (@WardId, @RoomNumber, @RoomTypeId, @IsActive)";
                _db.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@WardId", room.WardId),
                    new SqlParameter("@RoomNumber", room.RoomNumber),
                    new SqlParameter("@RoomTypeId", room.RoomTypeId),
                    new SqlParameter("@IsActive", room.IsActive)
                });
            }
            else
            {
                string sql = "UPDATE Rooms SET WardId=@WardId, RoomNumber=@RoomNumber, RoomTypeId=@RoomTypeId, IsActive=@IsActive WHERE RoomId=@RoomId";
                _db.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@RoomId", room.RoomId),
                    new SqlParameter("@WardId", room.WardId),
                    new SqlParameter("@RoomNumber", room.RoomNumber),
                    new SqlParameter("@RoomTypeId", room.RoomTypeId),
                    new SqlParameter("@IsActive", room.IsActive)
                });
            }
        }

        // --- Beds ---
        public List<Bed> GetBeds()
        {
            string sql = @"
                SELECT b.*, r.RoomNumber, w.WardName 
                FROM Beds b
                LEFT JOIN Rooms r ON b.RoomId = r.RoomId
                LEFT JOIN Wards w ON r.WardId = w.WardId
                WHERE b.IsActive = 1";

            return _db.ExecuteQuery(sql, r => new Bed
            {
                BedId = (int)r["BedId"],
                RoomId = r["RoomId"] != DBNull.Value ? (int)r["RoomId"] : 0,
                BedNumber = r["BedNumber"].ToString()!,
                Status = r["Status"].ToString()!,
                IsActive = (bool)r["IsActive"],
                RoomNumber = r["RoomNumber"]?.ToString(),
                WardName = r["WardName"]?.ToString()
            });
        }

        public void SaveBed(Bed bed)
        {
            if (bed.BedId == 0)
            {
                string sql = "INSERT INTO Beds (RoomId, BedNumber, Status, IsActive) VALUES (@RoomId, @BedNumber, @Status, @IsActive)";
                _db.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@RoomId", bed.RoomId),
                    new SqlParameter("@BedNumber", bed.BedNumber),
                    new SqlParameter("@Status", bed.Status),
                    new SqlParameter("@IsActive", bed.IsActive)
                });
            }
            else
            {
                string sql = "UPDATE Beds SET RoomId=@RoomId, BedNumber=@BedNumber, Status=@Status, IsActive=@IsActive WHERE BedId=@BedId";
                _db.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@BedId", bed.BedId),
                    new SqlParameter("@RoomId", bed.RoomId),
                    new SqlParameter("@BedNumber", bed.BedNumber),
                    new SqlParameter("@Status", bed.Status),
                    new SqlParameter("@IsActive", bed.IsActive)
                });
            }
        }

        // --- Admissions ---
        public void AdmitPatient(Admission admission)
        {
            // Update Bed Status first
            string updateBedSql = "UPDATE Beds SET Status = 'Occupied' WHERE BedId = @BedId";
            _db.ExecuteNonQuery(updateBedSql, new[] { new SqlParameter("@BedId", admission.BedId) });

            // Create Admission
            string sql = @"
                INSERT INTO Admissions (PatientId, BedId, AdmissionDate, Status, Notes) 
                VALUES (@PatientId, @BedId, @AdmissionDate, 'Admitted', @Notes)";

            _db.ExecuteNonQuery(sql, new[] {
                new SqlParameter("@PatientId", admission.PatientId),
                new SqlParameter("@BedId", admission.BedId),
                new SqlParameter("@AdmissionDate", admission.AdmissionDate),
                new SqlParameter("@Notes", (object?)admission.Notes ?? DBNull.Value)
            });
        }

        public void DischargePatient(int admissionId)
        {
            // Get BedId first
            var bedIdObj = _db.ExecuteScalar("SELECT BedId FROM Admissions WHERE AdmissionId = @Id", new[] { new SqlParameter("@Id", admissionId) });
            if (bedIdObj != null && int.TryParse(bedIdObj.ToString(), out int bedId))
            {
                // Update Bed Status to Cleaning
                _db.ExecuteNonQuery("UPDATE Beds SET Status = 'Cleaning' WHERE BedId = @BedId", new[] { new SqlParameter("@BedId", bedId) });
            }

            // Update Admission
            string sql = "UPDATE Admissions SET DischargeDate = GETDATE(), Status = 'Discharged' WHERE AdmissionId = @Id";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@Id", admissionId) });
        }

        public List<Admission> GetActiveAdmissions()
        {
            string sql = @"
                SELECT a.*, b.BedNumber, w.WardName, rt.TypeName as RoomTypeName, rt.DailyRate, p.FullName as PatientName
                FROM Admissions a
                JOIN Beds b ON a.BedId = b.BedId
                LEFT JOIN Rooms r ON b.RoomId = r.RoomId
                LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
                LEFT JOIN Wards w ON r.WardId = w.WardId
                LEFT JOIN Patients p ON a.PatientId = p.PatientId
                WHERE a.Status = 'Admitted'";

            return _db.ExecuteQuery(sql, r => new Admission
            {
                AdmissionId = (int)r["AdmissionId"],
                PatientId = (int)r["PatientId"],
                BedId = (int)r["BedId"],
                AdmissionDate = (DateTime)r["AdmissionDate"],
                Status = r["Status"].ToString()!,
                Notes = r["Notes"]?.ToString() ?? "",
                BedNumber = r["BedNumber"]?.ToString(),
                WardName = r["WardName"]?.ToString(),
                RoomTypeName = r["RoomTypeName"]?.ToString(),
                DailyRate = r["DailyRate"] != DBNull.Value ? (decimal)r["DailyRate"] : 0,
                PatientName = r.Table.Columns.Contains("PatientName") ? r["PatientName"]?.ToString() : "Unknown"
            });
        }
    }
}
