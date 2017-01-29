using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using QC = System.Data.SqlClient;
using CG = System.Collections.Generic;
using TD = System.Threading;

namespace KnowYourself.Controllers
{
    public class DBConnection {

        private QC.SqlConnection conn;

        public DBConnection()
        {
            conn = getConnection();
            conn.Open();

        }

        static QC.SqlConnection getConnection()
        {
            return new QC.SqlConnection(@"Server = tcp:knowyourself.database.windows.net, 1433; 
                    Initial Catalog = KnowYourself; Persist Security Info = False; 
                    User ID = su ; Password = !alexandrU97 ; 
                    MultipleActiveResultSets = True; Encrypt = True; 
                    TrustServerCertificate = False; 
                    Connection Timeout = 30");
            
        }

      

        public List<String> InsertUser(string slack_id)
        {
            
            List<String> messages = new List<String>();
            try
            {
                var command2 = new QC.SqlCommand();
                command2.Connection = this.conn;
                command2.CommandType = System.Data.CommandType.Text;
                command2.CommandText = @"
                    INSERT INTO users (slack_id)
                    VALUES (@slack_id)
                    ";
                var param2 = new QC.SqlParameter("slack_id", slack_id);
                command2.Parameters.Add(param2);
                int succ = command2.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                messages.Add(e.Message);
            }
            return messages;
        }

        public List<String> GetDiseases()
        {
            List<String> ret = new List<String>();

            try
            {
                QC.SqlCommand com = new QC.SqlCommand();
                com.Connection = this.conn;
                com.CommandType = System.Data.CommandType.Text;
                com.CommandText = @"SELECT disease_name FROM diseases";
                QC.SqlDataReader result = com.ExecuteReader();
                while (result.Read())
                {
                    ret.Add(result.GetString(0));
                }
            }catch(Exception e)
            {
                ret.Add(e.Message);
            }


            return ret;
        }

        public bool UserExists(string slack_id)
        {
            QC.SqlCommand exists = new QC.SqlCommand();
            exists.CommandType = System.Data.CommandType.Text;
            exists.Connection = this.conn;
            exists.CommandText = @"SELECT * FROM users WHERE slack_id = @User";

            var param1 = new QC.SqlParameter("User", slack_id);
            exists.Parameters.Add(param1);

            try
            {
                int id = (int)exists.ExecuteScalar();
                if (id > 0) return true;
               
            }
            catch (Exception e)
            {
                return false;
            }

            return false;
        }

        public void AddUserDisease(String slack_id, String disease_name)
        {
            QC.SqlCommand selectID = new QC.SqlCommand();
            selectID.Connection = this.conn;
            selectID.CommandType = System.Data.CommandType.Text;
            selectID.CommandText = @"SELECT id FROM users WHERE slack_id = @Name";
            var s_id = new QC.SqlParameter("Name", slack_id);
            selectID.Parameters.Add(s_id);

            QC.SqlCommand selectDisease = new QC.SqlCommand();
            selectDisease.Connection= this.conn;
            selectDisease.CommandType = System.Data.CommandType.Text;
            selectDisease.CommandText = @"SELECT id FROM diseases WHERE disease_name = @Name";
            var d_id = new QC.SqlParameter("Name",disease_name);

            selectDisease.Parameters.Add(d_id);

            try
            {
                int id = (int) selectID.ExecuteScalar();
                int ds_id = (int)selectDisease.ExecuteScalar();

                QC.SqlCommand inserter = new QC.SqlCommand();
                inserter.CommandType = System.Data.CommandType.Text;
                inserter.Connection = this.conn;
                inserter.CommandText = @"INSERT INTO user_diseases (user_id, disease_id)
                                         VALUES (@id1, @id2)";
                var id1 = new QC.SqlParameter("id1", id);
                var id2 = new QC.SqlParameter("id2", ds_id);
                inserter.Parameters.Add(id1);
                inserter.Parameters.Add(id2);

                inserter.ExecuteNonQuery();

            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public Dictionary<int, String> GetQuestionsForUser(string slack_id)
        {
            Dictionary<int, String> ret = new Dictionary<int, String>();
            QC.SqlCommand comm = new QC.SqlCommand();
            comm.Connection = this.conn;
            comm.CommandType = System.Data.CommandType.Text;
            comm.CommandText = @"
                
                SELECT Q.*
                FROM disease_questions DQ, diseases D, questions Q, user_diseases UD, users U
                WHERE
                    U.slack_id = @Name AND
                    U.id = UD.user_id AND
                    UD.disease_id = D.id AND
                    D.id = DQ.disease_id AND
                    DQ.question_id = Q.id
            ";
            var param1 = new QC.SqlParameter("Name", slack_id);
            comm.Parameters.Add(param1);

            var response = comm.ExecuteReader();

            while (response.Read())
            {
                ret.Add(response.GetInt32(0), response.GetString(1));
            }

            return ret;
        } 

        public void StoreAnswerToDB(string answer, string slack_id, int questionID)
        {
            DateTime time = new DateTime();

            String timeS = $"{time.Year}-{time.Month}-{time.Day}";

            QC.SqlCommand getID = new QC.SqlCommand();
            getID.Connection = this.conn;
            getID.CommandType = System.Data.CommandType.Text;
            getID.CommandText = "SELECT id FROM users WHERE slack_id = @Name";

            var param1 = new QC.SqlParameter("Name", slack_id);
            getID.Parameters.Add(param1);

            try
            {
                int id = (int)getID.ExecuteScalar();
                QC.SqlCommand inserter = new QC.SqlCommand();
                inserter.Connection = this.conn;
                inserter.CommandType = System.Data.CommandType.Text;
                inserter.CommandText = @"
                    INSERT INTO answers (question_id, answer,user_id, date)
                    VALUES (@id, @ans, @usr, @date)
                ";

                var param2 = new QC.SqlParameter("id", questionID);
                var param3 = new QC.SqlParameter("ans", answer);
                var param4 = new QC.SqlParameter("usr", id);
                var param5 = new QC.SqlParameter("date", timeS);

                inserter.Parameters.Add(param2);
                inserter.Parameters.Add(param3);
                inserter.Parameters.Add(param4);
                inserter.Parameters.Add(param5);

                inserter.ExecuteNonQuery();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }


    }


}


