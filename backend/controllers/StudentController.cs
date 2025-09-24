using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace ProjectRoot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController(IConfiguration configuration) : ControllerBase
    {
        private readonly string connectionString = configuration.GetConnectionString("OracleDb");

        [HttpGet("GetGenders")]
        public IActionResult GetGenders()
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand("SELECT gender_id, gender FROM system.gender", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var result = new List<object>();
                    while (reader.Read())
                    {
                        result.Add(new
                        {
                            GenderId = reader.GetInt32(0),
                            Gender = reader.GetString(1)
                        });
                    }
                    return Ok(result);
                }
            }
        }

        [HttpGet("GetQualifications")]
        public IActionResult GetQualifications()
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand("SELECT q_id, q_name FROM system.qualification", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var result = new List<object>();
                    while (reader.Read())
                    {
                        result.Add(new
                        {
                            QualificationId = reader.GetInt32(0),
                            Qualification = reader.GetString(1)
                        });
                    }
                    return Ok(result);
                }
            }
        }

        [HttpGet("GetModes")]
        public IActionResult GetModes()
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand("SELECT m_id, m_name FROM system.mode_of_study", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var result = new List<object>();
                    while (reader.Read())
                    {
                        result.Add(new
                        {
                            ModeId = reader.GetInt32(0),
                            Mode = reader.GetString(1)
                        });
                    }
                    return Ok(result);
                }
            }
        }
        [HttpGet("GetAllStudents")]
        public IActionResult GetAllStudents()
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(
                    @"SELECT s.s_id, s.full_name, s.email, s.phone_no, g.gender, q.q_name, m.m_name, s.start_date
              FROM system.student_reg s
              JOIN system.gender g ON s.gender_id = g.gender_id
              JOIN system.qualification q ON s.q_id = q.q_id
              JOIN system.mode_of_study m ON s.mode_id = m.m_id", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var result = new List<object>();
                    while (reader.Read())
                    {
                        result.Add(new
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            Phone = reader.GetString(3),
                            Gender = reader.GetString(4),
                            Qualification = reader.GetString(5),
                            Mode = reader.GetString(6),
                            StartDate = reader.GetDateTime(7).ToString("dd-MM-yyyy")
                        });
                    }
                    return Ok(result);
                }
            }
        }


        [HttpPost("addstudent")]
        public IActionResult AddStudent(Student student)
        {
            using (var conn = new OracleConnection(connectionString))
            using (var cmd = new OracleCommand("STUDENT_PKG.INSERT_STUDENT", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Output parameter (must come first or last, order doesn’t matter if names match)
                var outId = new OracleParameter("P_S_ID", OracleDbType.Int32)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outId);

                // Input parameters (names MUST match)
                cmd.Parameters.Add("P_FULL_NAME", OracleDbType.Varchar2).Value = student.FullName;
                cmd.Parameters.Add("P_EMAIL", OracleDbType.Varchar2).Value = student.Email;
                cmd.Parameters.Add("P_PHONE_NO", OracleDbType.Varchar2).Value = student.PhoneNumber;
                cmd.Parameters.Add("P_ADDRESS", OracleDbType.Varchar2).Value = student.Address;
                cmd.Parameters.Add("P_DOB", OracleDbType.Date).Value = student.DateOfBirth;
                cmd.Parameters.Add("P_GENDER_ID", OracleDbType.Int32).Value = student.GenderId;
                cmd.Parameters.Add("P_Q_ID", OracleDbType.Int32).Value = student.QualificationId;
                cmd.Parameters.Add("P_MODE_ID", OracleDbType.Int32).Value = student.ModeId;
                cmd.Parameters.Add("P_START_DATE", OracleDbType.Date).Value = student.CourseStartDate;

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return Ok(new { StudentId = outId.Value });
                }
                catch (OracleException oex)
                {
                    // For RAISE_APPLICATION_ERROR, OracleException.Number contains the custom error code
                    return StatusCode(400, new
                    {
                        OracleErrorCode = oex.Number,
                        OracleErrorMessage = oex.Message
                    });
                }
                catch (Exception ex)
                {
                    // Fallback for non-Oracle errors
                    return StatusCode(500, new
                    {
                        Error = ex.Message
                    });
                }

            }
        }


        [HttpDelete("deletestudent")]
        public IActionResult DeleteStudent([FromBody] DeleteStudentRequest req)
        {
            if (req == null || req.Id == null)
                return BadRequest(new { error = "Id is required." });

            using var conn = new OracleConnection(connectionString);
            using var cmd = new OracleCommand("STUDENT_PKG.DELETE_STUDENT", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("P_S_ID", OracleDbType.Int32).Value = req.Id;

            try
            {
                conn.Open();
                var rows = cmd.ExecuteNonQuery();

                // Many Oracle stored procedures won’t return affected rows; assume success if no exception
                return Ok(new { message = $"Student with id {req.Id} deleted", id = req.Id });
            }
            catch (OracleException oex)
            {
                // Map Oracle errors appropriately if needed
                return StatusCode(400, new { OracleErrorCode = oex.Number, OracleErrorMessage = oex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class DeleteStudentRequest
        {
            public int? Id { get; set; }
        }

    }
    public class Student
    {
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Address { get; set; }
        public required DateTime DateOfBirth { get; set; }
        public required int GenderId { get; set; }
        public required int QualificationId { get; set; }
        public required int ModeId { get; set; }
        public required DateTime CourseStartDate { get; set; }
    }
}
