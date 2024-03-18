using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace comp3005a3q1
{
    internal class Program {
        static void Main(string[] args)
        {
            if(args.Length != 1) {
                Console.WriteLine($"Usage: {System.Environment.GetCommandLineArgs()[0]} <postgresql connection string>");
                return;
            }

            using(var studentManager = new StudentManager(args[0])) {
                // Get all students
                foreach(Student s in studentManager.getAllStudents()) {
                    Console.WriteLine($"{s.student_id}: {s.first_name} {s.last_name}; email is {s.email}"
                        + (s.enrollment_date.HasValue ? $", enrollment date is {s.enrollment_date}" : ""));
                }

                // Add a student
                Console.Write("Enter new student's first name: ");
                var newStuFirstName = Console.ReadLine()!;
                Console.Write("Enter new student's last name: ");
                var newStuLastName = Console.ReadLine()!;
                Console.Write("Enter new student's email: ");
                var newStuEmail = Console.ReadLine()!;
                studentManager.addStudent(newStuFirstName, newStuLastName, newStuEmail, DateTime.UtcNow).Wait();
                Console.WriteLine("Student added!");

                // Update student email
                Console.Write("Enter student ID to update email of: ");
                var idToUpdate = int.Parse(Console.ReadLine() ?? "1"); // Default to ID 1 if input invalid
                Console.Write("Enter new email for student: ");
                var newEmail = Console.ReadLine() ?? "";
                studentManager.updateStudentEmail(idToUpdate, newEmail).Wait();
                Console.WriteLine("Student email updated!");

                // Delete student
                int idToDelete;
                while(true) {
                    Console.Write("Enter student ID to delete: ");
                    var input = Console.ReadLine();
                    if(input == null)
                        continue;
                    try {
                        idToDelete = int.Parse(input);
                    } catch (Exception) {
                        continue;
                    }
                    break;
                }
                studentManager.deleteStudent(idToDelete).Wait();

                // Get all students, again
                foreach(Student s in studentManager.getAllStudents()) {
                    Console.WriteLine($"{s.student_id}: {s.first_name} {s.last_name}; email is {s.email}"
                        + (s.enrollment_date.HasValue ? $", enrollment date is {s.enrollment_date}" : ""));
                }
            }
        }
    }

    class StudentManager(string connectionString) : IDisposable {
        StudentContext ctx = new StudentContext(connectionString);

        // Retrieves and displays all records from the students table.
        public IEnumerable<Student> getAllStudents() {
            return ctx.students;
        }

        // Inserts a new student record into the students table.
        public async Task addStudent(string first_name, string last_name, string email, DateTime enrollment_date) {
            var newStudent = new Student() {
                first_name = first_name,
                last_name = last_name,
                email = email,
                enrollment_date = enrollment_date
            };
            await ctx.students.AddAsync(newStudent);
            await ctx.SaveChangesAsync();
        }

        // Updates the email address for a student with the specified student_id.
        public Task updateStudentEmail(int student_id, string new_email)
            => ctx.students
                .Where(s => s.student_id == student_id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.email, new_email));

        // Deletes the record of the student with the specified student_id.
        public Task deleteStudent(int student_id) 
            => ctx.students
                .Where(s => s.student_id == student_id)
                .ExecuteDeleteAsync();

        public void Dispose() {
            ctx.SaveChanges();
        }
    }

    class StudentContext(string connectionString) : DbContext {
        public DbSet<Student> students { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(connectionString);
    }

    class Student {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int student_id { get; set; }
        public required string first_name { get; set; }
        public required string last_name { get; set; }
        public required string email { get; set; }
        public DateTime? enrollment_date { get; set; }
    }
}