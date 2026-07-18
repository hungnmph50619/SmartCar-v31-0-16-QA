namespace SmartCar.WebUI.Helpers
{
    public class BlogArticleSection
    {
        public string Heading { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class BlogArticleContent
    {
        public string DisplayTitle { get; set; } = string.Empty;
        public string Introduction { get; set; } = string.Empty;
        public List<BlogArticleSection> Sections { get; set; } = new();
    }

    public static class BlogArticleContentBuilder
    {
        private sealed record TopicSeed(string Title, string Focus, string Practice, string Risk, string Checklist);
        private sealed record Family(string[] Keywords, string[] Headings, TopicSeed[] Seeds);

        private static readonly Family[] Families =
        {
            new(
                new[] { "cung đường", "du lịch", "hành trình" },
                new[] { "Lập kế hoạch cung đường", "Phân bổ thời gian và điểm dừng", "Rủi ro cần chuẩn bị", "Danh sách kiểm tra trước khi đi" },
                new TopicSeed[]
                {
                    new("Kinh nghiệm lái xe dọc cung đường ven biển miền Trung", "Các tuyến ven biển đẹp nhưng thường có gió ngang, đoạn dân cư và nhiều nút giao nhỏ. Nên ưu tiên lộ trình ban ngày để quan sát tốt và có thời gian dừng ngắm cảnh.", "Chia hành trình thành các chặng 120–180 km, đặt trước điểm nghỉ và kiểm tra trạm nhiên liệu hoặc trạm sạc trước khi rời đô thị lớn.", "Không nên chạy sát xe tải, dừng xe tùy tiện ở lề hẹp hoặc phụ thuộc hoàn toàn vào chỉ dẫn của bản đồ khi đường đang sửa chữa.", "Giấy tờ xe, lốp dự phòng, nước uống, pin điện thoại, bản đồ ngoại tuyến và số cứu hộ."),
                    new("Lịch trình tự lái Hà Nội – Ninh Bình trong một ngày", "Tuyến Hà Nội – Ninh Bình phù hợp chuyến đi ngắn, nhưng cần tính thời gian qua cửa ngõ phía Nam và thời gian gửi xe tại khu du lịch.", "Khởi hành sớm, chọn một cụm điểm tham quan gần nhau và để ít nhất 90 phút dự phòng cho chiều về.", "Lịch trình quá dày dễ khiến người lái mệt, về muộn hoặc phải chạy nhanh để kịp kế hoạch.", "Nhiên liệu đầy, tiền phí đường bộ, áo mưa, giày dễ đi và vị trí bãi đỗ tại điểm đến."),
                    new("Chuẩn bị xe cho chuyến đi Sa Pa mùa lạnh", "Đường lên Sa Pa có đèo dốc, sương mù và nhiệt độ thấp. Khả năng quan sát và tình trạng lốp quan trọng hơn tốc độ.", "Kiểm tra phanh, nước rửa kính, đèn sương mù và độ sâu gai lốp; xuống dốc bằng số thấp thay vì rà phanh liên tục.", "Không vượt ở khúc cua khuất, không dừng giữa đèo để chụp ảnh và không chạy khi kính bị mờ.", "Áo ấm, găng tay, khăn lau kính, đèn pin, thuốc cá nhân và số điện thoại cứu hộ địa phương."),
                    new("Tự lái Đà Nẵng – Hội An – Huế sao cho hợp lý", "Ba điểm đến gần nhau nhưng điều kiện đường khác nhau: đô thị đông, cung ven biển và đèo Hải Vân.", "Dành riêng từng ngày cho Huế và Hội An; chọn đi hầm Hải Vân khi thời tiết xấu hoặc đi đèo khi trời quang và người lái đủ kinh nghiệm.", "Chạy theo lịch quá sát dễ gặp kẹt xe tại trung tâm, thiếu chỗ đỗ hoặc lái đèo lúc trời tối.", "Kiểm tra thời tiết, nơi đỗ xe, giờ cấm xe, phí tham quan và thời gian nhận trả xe."),
                    new("Kinh nghiệm lái xe ở cao nguyên nhiều sương", "Đường cao nguyên có đoạn tầm nhìn giảm nhanh, mặt đường ẩm và nhiều xe máy đi sát lề.", "Bật đèn chiếu gần, giữ khoảng cách xa hơn bình thường, giảm tốc trước cua và tránh bám theo đèn hậu xe trước quá sát.", "Đèn pha gây phản xạ trong sương; phanh gấp và chuyển làn đột ngột dễ làm xe mất ổn định.", "Đèn xe, gạt mưa, sấy kính, lốp, phanh và lịch nghỉ của người lái."),
                    new("Lập kế hoạch road trip Tây Nguyên 4 ngày", "Khoảng cách giữa các thành phố Tây Nguyên khá dài, một số đoạn ít dịch vụ và sóng điện thoại không ổn định.", "Mỗi ngày chỉ nên bố trí một chặng chính, đổ nhiên liệu khi bình còn trên một phần ba và tải bản đồ ngoại tuyến.", "Đi đêm trên đường lạ, bỏ qua cảnh báo thời tiết hoặc chọn đường tắt chưa kiểm chứng có thể kéo dài hành trình.", "Lịch trình in sẵn, tiền mặt, bộ vá lốp, nước uống, thuốc chống say và danh sách cơ sở sửa xe."),
                    new("Chọn xe cho chuyến cắm trại cuối tuần", "Chuyến cắm trại cần quan tâm khoang hành lý, khoảng sáng gầm và khả năng đi đường xấu nhẹ, không chỉ số chỗ ngồi.", "Xếp đồ nặng sát lưng ghế, cố định bình gas và vật sắc nhọn, kiểm tra tải trọng trước khi xuất phát.", "Chở đồ che kính sau, buộc hành lý trên nóc không đúng cách hoặc đi vào đường trơn vượt khả năng xe là các lỗi phổ biến.", "Lều, đèn, dây kéo, bơm lốp, túi rác, bộ sơ cứu và vị trí trạm y tế gần khu cắm trại."),
                    new("Kinh nghiệm đưa trẻ nhỏ đi xa bằng ô tô", "Trẻ nhỏ cần ghế an toàn đúng độ tuổi, lịch nghỉ đều và nhiệt độ khoang xe ổn định.", "Lắp ghế trẻ em trước chuyến đi, chuẩn bị đồ ăn nhẹ và dừng nghỉ sau 90–120 phút.", "Bế trẻ trên tay khi xe chạy, để trẻ một mình trong xe hoặc đặt đồ cứng trên kệ sau đều nguy hiểm.", "Ghế trẻ em, khăn ướt, nước, quần áo dự phòng, thuốc cá nhân và đồ chơi mềm."),
                    new("Tổ chức chuyến đi nhóm bằng hai xe", "Đi nhiều xe cần thống nhất lộ trình, điểm tập kết và cách liên lạc thay vì cố bám đuôi nhau liên tục.", "Chỉ định xe dẫn đoàn, chia sẻ vị trí theo thời gian thực và hẹn điểm dừng sau từng chặng.", "Vượt ẩu để nhập đoàn, dừng đột ngột hoặc gọi điện khi đang lái xe làm tăng rủi ro.", "Danh sách thành viên, số xe, số điện thoại, bộ đàm hoặc ứng dụng liên lạc và kế hoạch nếu tách đoàn."),
                    new("Lái xe đi biển mùa cao điểm tránh mệt mỏi", "Mùa cao điểm thường ùn tắc ở cửa ngõ, bãi đỗ đầy và nhiệt độ cao làm người lái nhanh mất sức.", "Xuất phát trước giờ cao điểm, đặt chỗ đỗ, mang đủ nước và nghỉ ngay khi có dấu hiệu buồn ngủ.", "Cố chạy liên tục, để áp suất lốp quá thấp hoặc để đồ điện tử dưới nắng lâu là các lỗi thường gặp.", "Kem chống nắng, nước, kính râm, bơm lốp, khăn che nắng và xác nhận nơi đỗ xe."),
                }),
            new(
                new[] { "gia đình", "lựa chọn xe", "chọn xe" },
                new[] { "Xác định đúng nhu cầu", "Không gian và trang bị cần có", "Chi phí cần tính đủ", "Gợi ý lựa chọn cuối cùng" },
                new TopicSeed[]
                {
                    new("Chọn xe 5 chỗ cho gia đình nhỏ", "Gia đình 3–4 người thường phù hợp sedan hoặc crossover cỡ nhỏ, nhưng cần tính thêm ghế trẻ em và hành lý.", "Ưu tiên điều hòa tốt, camera lùi, cốp đủ rộng và hàng ghế sau có điểm gắn ghế trẻ em.", "Chọn xe chỉ theo giá rẻ có thể dẫn đến thiếu không gian hoặc tiêu hao nhiên liệu không phù hợp hành trình.", "So sánh ít nhất ba mẫu theo số người, số vali, quãng đường và vị trí đỗ xe."),
                    new("Khi nào gia đình nên thuê xe 7 chỗ", "Xe 7 chỗ cần thiết khi đi từ 5 người trở lên hoặc có nhiều hành lý, nhưng hàng ghế ba không phải mẫu nào cũng thoải mái.", "Kiểm tra lối vào hàng ghế ba, cửa gió điều hòa, số cổng sạc và dung tích cốp khi đủ người.", "Xếp quá nhiều hành lý chắn tầm nhìn hoặc để người lớn ngồi hàng ghế ba quá lâu gây mệt mỏi.", "Chọn MPV nếu ưu tiên không gian, SUV nếu cần gầm cao và đường hỗn hợp."),
                    new("Sedan hay SUV cho chuyến du lịch gia đình", "Sedan êm và tiết kiệm trên đường tốt; SUV có tầm nhìn cao và thuận lợi hơn khi đường xấu nhẹ.", "Đánh giá điều kiện đường, khả năng lên xuống xe của người già và kích thước bãi đỗ.", "Thuê SUV cỡ lớn trong phố đông có thể khó đỗ, còn sedan gầm thấp không phù hợp đường ngập hoặc ổ gà sâu.", "Chọn dựa trên hành trình thực tế thay vì hình thức bên ngoài."),
                    new("Chọn xe cho gia đình có người cao tuổi", "Người cao tuổi cần bậc lên xuống vừa phải, ghế ngồi êm và không gian chân tốt.", "Ưu tiên xe cửa rộng, ghế không quá thấp, điều hòa dễ chỉnh và giảm xóc êm.", "Xe gầm quá cao, hàng ghế chật hoặc hành trình không có điểm nghỉ dễ gây mệt và đau khớp.", "Cho người sử dụng thử lên xuống xe trước khi xác nhận."),
                    new("Chọn xe cho gia đình có hai trẻ nhỏ", "Hai ghế trẻ em chiếm nhiều chiều rộng hàng ghế sau và cần đủ điểm ISOFIX hoặc dây đai phù hợp.", "Kiểm tra khoảng trống giữa hai ghế, cửa mở rộng và cốp chứa được xe đẩy.", "Đặt ghế trẻ em không đúng hướng, dùng đệm tự chế hoặc để vật nặng cạnh trẻ là nguy cơ lớn.", "Mang ghế trẻ em đến thử hoặc hỏi rõ kích thước hàng ghế trước khi thuê."),
                    new("Xe nào phù hợp chuyến về quê dịp lễ", "Chuyến về quê thường nhiều hành lý, đường đông và thời gian di chuyển dài hơn bình thường.", "Ưu tiên xe có cốp lớn, điều hòa ổn định, ghế ngồi thoải mái và mức tiêu hao hợp lý.", "Chở quá tải, buộc đồ lỏng trên nóc hoặc chọn giờ xuất phát trùng cao điểm làm chuyến đi căng thẳng.", "Xếp hành lý từ tối hôm trước và chừa không gian quan sát phía sau."),
                    new("Chọn xe cho nhóm bạn 6 người", "Sáu người lớn cần một MPV hoặc SUV 7 chỗ thực sự có hàng ghế ba dùng được.", "Kiểm tra chỗ để hành lý khi mở đủ ghế, điều hòa phía sau và độ rộng cửa.", "Một số xe 7 chỗ chỉ phù hợp trẻ em ở hàng ba; thuê nhầm sẽ gây mệt trên đường dài.", "Ưu tiên MPV nếu đi đường tốt và nhiều hành lý."),
                    new("Cân nhắc xe hybrid cho gia đình", "Xe hybrid tiết kiệm trong đô thị và vận hành êm, nhưng người thuê cần làm quen chế độ lái và hiển thị năng lượng.", "Hỏi rõ cách khởi động, phanh tái sinh và mức nhiên liệu khi bàn giao.", "Tưởng xe chưa nổ máy vì động cơ xăng im lặng hoặc để pin phụ cạn do dùng điện khi xe tắt là lỗi thường gặp.", "Phù hợp gia đình đi phố nhiều và muốn giảm chi phí nhiên liệu."),
                    new("Chọn xe nhỏ khi đi phố đông", "Hatchback hoặc sedan cỡ nhỏ dễ xoay trở, tiết kiệm và phù hợp bãi đỗ hạn chế.", "Kiểm tra cốp, khoảng để chân hàng sau và camera lùi trước khi chọn.", "Xe nhỏ không phù hợp nhóm đông người, nhiều vali hoặc hành trình đường xấu.", "Đánh đổi hợp lý giữa sự linh hoạt và không gian sử dụng."),
                    new("Thuê xe cao cấp cho dịp đặc biệt", "Xe cao cấp phù hợp cưới hỏi, đón đối tác hoặc sự kiện khi hình ảnh và trải nghiệm là ưu tiên.", "Kiểm tra quy định sử dụng, giới hạn quãng đường, mức đặt cọc và bảo hiểm.", "Chi phí vệ sinh, phụ thu quá giờ hoặc yêu cầu bàn giao nghiêm ngặt thường cao hơn xe phổ thông.", "Chỉ chọn khi mục đích rõ ràng và ngân sách đã bao gồm chi phí phát sinh."),
                }),
            new(
                new[] { "nhiên liệu", "tiết kiệm xăng", "tiêu hao" },
                new[] { "Nguyên nhân làm xe tốn nhiên liệu", "Kỹ thuật lái nên áp dụng", "Bảo dưỡng ảnh hưởng thế nào", "Cách theo dõi hiệu quả" },
                new TopicSeed[]
                {
                    new("Giữ tốc độ ổn định để giảm tiêu hao nhiên liệu", "Tăng tốc mạnh rồi phanh liên tục làm động cơ hoạt động ngoài vùng hiệu quả.", "Quan sát xa, giữ chân ga đều và giảm tốc sớm khi gần giao lộ.", "Bám xe quá sát khiến người lái phải phanh và tăng tốc nhiều lần.", "Theo dõi mức tiêu hao trung bình sau từng chặng thay vì chỉ nhìn tức thời."),
                    new("Áp suất lốp ảnh hưởng đến mức tiêu hao ra sao", "Lốp thiếu hơi tăng lực cản lăn và làm xe nặng nề hơn.", "Đo áp suất khi lốp nguội và bơm theo thông số trên khung cửa.", "Bơm quá căng làm giảm độ bám và mòn giữa lốp.", "Kiểm tra ít nhất mỗi hai tuần hoặc trước chuyến đi dài."),
                    new("Dùng điều hòa ô tô tiết kiệm mà vẫn thoải mái", "Điều hòa đặt quá lạnh làm máy nén hoạt động liên tục.", "Làm thoáng cabin trước, chọn 24–26°C và bật chế độ tuần hoàn khi phù hợp.", "Tắt điều hòa hoàn toàn trong trời nóng có thể làm người lái mệt và mất tập trung.", "Đỗ nơi râm, dùng tấm chắn nắng và vệ sinh lọc gió định kỳ."),
                    new("Xếp hành lý đúng cách để xe nhẹ và an toàn", "Khối lượng dư thừa khiến động cơ tiêu hao nhiều hơn, nhất là khi dừng và tăng tốc.", "Chỉ mang đồ cần thiết, đặt vật nặng thấp và sát tâm xe.", "Chở đồ trên nóc tăng cản gió đáng kể và phải cố định đúng kỹ thuật.", "Tháo giá nóc khi không dùng và kiểm tra tải trọng cho phép."),
                    new("Lựa chọn tuyến đường giúp tiết kiệm xăng", "Đường ngắn nhất chưa chắc tiết kiệm nếu thường xuyên ùn tắc hoặc nhiều đèn đỏ.", "So sánh thời gian, tốc độ trung bình và phí đường bộ trước khi đi.", "Đổi lộ trình liên tục theo ứng dụng có thể đưa xe vào đường nhỏ khó đi.", "Ưu tiên tuyến ổn định, ít dừng và phù hợp loại xe."),
                    new("Khi nào nên dùng chế độ Eco", "Eco làm phản hồi ga dịu hơn và tối ưu điều hòa, phù hợp giao thông đều.", "Dùng khi đi phố hoặc cao tốc ổn định, tắt khi cần vượt dứt khoát hoặc leo dốc nặng.", "Phụ thuộc hoàn toàn vào Eco nhưng vẫn tăng tốc gấp sẽ không tiết kiệm.", "Kết hợp Eco với quan sát xa và giữ tốc độ đều."),
                    new("Nổ máy chờ lâu có tốn nhiên liệu không", "Động cơ vẫn tiêu thụ nhiên liệu khi xe đứng yên và không tạo ra quãng đường.", "Nếu dừng lâu ở nơi an toàn, nên tắt máy theo hướng dẫn của xe.", "Tắt mở liên tục trong khoảng dừng rất ngắn có thể không cần thiết.", "Tránh nổ máy chỉ để dùng điều hòa trong thời gian dài."),
                    new("Bảo dưỡng lọc gió giúp xe vận hành hiệu quả", "Lọc gió bẩn làm giảm lưu lượng khí và ảnh hưởng quá trình đốt cháy.", "Kiểm tra theo lịch hoặc sớm hơn khi thường đi đường bụi.", "Tự vệ sinh sai cách có thể làm rách lọc hoặc đưa bụi vào đường nạp.", "Thay đúng chủng loại và ghi lại thời điểm bảo dưỡng."),
                    new("Lái xe đường cao tốc tiết kiệm nhiên liệu", "Ở tốc độ cao, lực cản không khí tăng nhanh và mức tiêu hao có thể tăng mạnh.", "Giữ tốc độ hợp lý, dùng ga đều và đóng cửa kính khi chạy nhanh.", "Tăng tốc để bám xe trước hoặc chuyển làn liên tục vừa tốn nhiên liệu vừa nguy hiểm.", "Dùng ga tự động nếu điều kiện đường và xe cho phép."),
                    new("Đọc chỉ số tiêu hao nhiên liệu đúng cách", "Mức tiêu hao tức thời dao động lớn và không phản ánh cả hành trình.", "Đặt lại đồng hồ khi bắt đầu chuyến đi và so sánh theo cùng điều kiện.", "So sánh xe khác nhau mà không tính tải, đường và thời tiết dễ dẫn đến kết luận sai.", "Ghi quãng đường, lượng nhiên liệu và tốc độ trung bình để đánh giá."),
                }),
            new(
                new[] { "thuê xe", "đặt xe", "hợp đồng" },
                new[] { "Chuẩn bị trước khi đặt", "Kiểm tra hợp đồng và chi phí", "Nhận xe đúng quy trình", "Xử lý phát sinh trong chuyến đi" },
                new TopicSeed[]
                {
                    new("Quy trình thuê xe tự lái lần đầu", "Người thuê lần đầu cần chuẩn bị giấy tờ, thời gian nhận trả và ngân sách đặt cọc.", "Đọc kỹ giá thuê, giới hạn quãng đường, nhiên liệu và điều kiện hủy.", "Không ký khi chưa kiểm tra hiện trạng hoặc để người không có tên trong hợp đồng lái xe.", "Chụp ảnh xe, đồng hồ nhiên liệu, giấy tờ và lưu số hỗ trợ."),
                    new("Cách đọc hợp đồng thuê xe tránh chi phí bất ngờ", "Hợp đồng cần ghi rõ giá, thời gian, phụ phí, trách nhiệm và phạm vi bảo hiểm.", "Hỏi về phí quá giờ, vệ sinh, giao nhận khác địa điểm và quãng đường vượt mức.", "Thỏa thuận miệng không được ghi lại dễ gây tranh chấp khi trả xe.", "Yêu cầu hóa đơn hoặc xác nhận điện tử cho mọi khoản thanh toán."),
                    new("Kinh nghiệm nhận xe ngoài giờ hành chính", "Nhận xe sớm hoặc muộn cần xác nhận người bàn giao và cách xử lý nếu có vấn đề.", "Kiểm tra ánh sáng đủ, quay video toàn bộ xe và thử các chức năng chính.", "Nhận xe trong bãi tối rồi bỏ qua vết xước có thể gây tranh cãi khi trả.", "Lưu số trực ca, biên bản điện tử và vị trí trả chìa khóa."),
                    new("Thuê xe theo ngày hay theo giờ", "Theo giờ phù hợp việc ngắn; theo ngày phù hợp hành trình dài và ít áp lực thời gian.", "Tính tổng thời gian thực tế gồm nhận trả, di chuyển và thời gian dự phòng.", "Chọn gói giờ quá sát dễ phát sinh phụ phí cao hơn gói ngày.", "So sánh tổng giá sau phụ phí chứ không chỉ đơn giá ban đầu."),
                    new("Đặt xe dịp lễ cần lưu ý gì", "Nhu cầu cao khiến xe tốt hết sớm và chính sách hủy thường chặt hơn.", "Đặt trước, xác nhận lại lịch và chuẩn bị phương án thay thế tương đương.", "Chuyển cọc cho tài khoản không xác minh hoặc tin quảng cáo giá quá thấp là rủi ro.", "Kiểm tra thông tin doanh nghiệp, hợp đồng và biên nhận cọc."),
                    new("Thuê xe một chiều giữa hai thành phố", "Trả xe khác nơi giúp tiết kiệm thời gian nhưng có phí điều chuyển.", "Xác nhận địa điểm, giờ trả, người nhận xe và mức phí một chiều.", "Tự thay đổi điểm trả vào phút cuối có thể làm phát sinh chi phí lớn.", "Lưu bằng chứng bàn giao và tình trạng xe tại điểm trả."),
                    new("Thuê xe cho công tác doanh nghiệp", "Doanh nghiệp cần hóa đơn, lịch trình rõ và xe phù hợp hình ảnh đối tác.", "Cung cấp danh sách người đi, điểm đón, thời gian và yêu cầu xuất hóa đơn từ đầu.", "Đặt xe sát giờ hoặc thay đổi nhiều lần làm khó việc điều phối.", "Chỉ định một đầu mối liên hệ và duyệt chi phí trước."),
                    new("Thuê xe khi chưa quen loại hộp số", "Người lái cần làm quen vị trí số, phanh tay và các chế độ hỗ trợ.", "Yêu cầu nhân viên hướng dẫn, chỉnh ghế gương và chạy thử trong khu vực an toàn.", "Nhầm chân phanh ga hoặc dùng sai chế độ xuống dốc có thể gây nguy hiểm.", "Chọn loại xe gần với xe thường dùng nếu chưa tự tin."),
                    new("Chính sách nhiên liệu khi thuê xe", "Phổ biến nhất là nhận mức nào trả mức đó hoặc nhận đầy trả đầy.", "Chụp đồng hồ nhiên liệu khi nhận và hỏi rõ cách tính nếu thiếu.", "Đổ sai loại nhiên liệu gây hư hỏng nghiêm trọng và không được bảo hiểm thông thường chi trả.", "Kiểm tra nhãn trên nắp bình và giữ hóa đơn đổ nhiên liệu cuối chuyến."),
                    new("Cách trả xe nhanh và minh bạch", "Trả xe thuận lợi khi khách chuẩn bị đúng giờ, đúng địa điểm và đầy đủ tài sản.", "Dọn đồ cá nhân, kiểm tra nhiên liệu, chụp ảnh và ký biên bản bàn giao.", "Rời đi trước khi xác nhận có thể khiến việc đối chiếu kéo dài.", "Giữ biên bản hoàn tất đến khi tiền cọc được hoàn lại."),
                }),
            new(
                new[] { "đường dài", "lái xe lâu", "cao tốc" },
                new[] { "Chuẩn bị thể lực và phương tiện", "Kỹ thuật lái trên chặng dài", "Dấu hiệu cần dừng nghỉ", "Kế hoạch khi có sự cố" },
                new TopicSeed[]
                {
                    new("Cách chống buồn ngủ khi lái xe đường dài", "Buồn ngủ làm phản xạ chậm tương tự sử dụng chất kích thích và không thể khắc phục bằng mở nhạc lớn.", "Ngủ đủ trước chuyến đi, đổi lái và nghỉ 15–20 phút sau mỗi hai giờ.", "Cố lái khi ngáp liên tục, lệch làn hoặc không nhớ đoạn đường vừa qua là rất nguy hiểm.", "Dừng tại nơi an toàn, uống nước và chỉ tiếp tục khi tỉnh táo."),
                    new("Giữ khoảng cách an toàn trên cao tốc", "Tốc độ cao làm quãng đường phanh tăng mạnh và xe trước có thể giảm tốc bất ngờ.", "Dùng quy tắc tối thiểu ba giây, tăng lên khi mưa hoặc tầm nhìn kém.", "Bám đuôi để tránh xe khác chen vào làm mất khoảng xử lý.", "Quan sát xa nhiều xe phía trước, không chỉ đèn hậu xe gần nhất."),
                    new("Vượt xe tải an toàn trên đường trường", "Xe tải có điểm mù lớn và luồng khí có thể làm xe con chao nhẹ.", "Chỉ vượt khi tầm nhìn đủ, báo hiệu sớm và hoàn thành dứt khoát trong giới hạn tốc độ.", "Chạy song song lâu hoặc cắt vào quá gần đầu xe tải là lỗi nguy hiểm.", "Đảm bảo nhìn thấy xe tải trong gương trước khi nhập làn."),
                    new("Lái xe khi trời mưa lớn", "Mưa giảm độ bám, tầm nhìn và có nguy cơ trượt nước.", "Giảm tốc, bật đèn chiếu gần, tăng khoảng cách và tránh phanh gấp.", "Bật đèn khẩn cấp khi xe vẫn chạy làm người khác khó nhận biết chuyển làn.", "Nếu tầm nhìn quá thấp, dừng ở nơi an toàn ngoài làn xe chạy."),
                    new("Xuống đèo đúng kỹ thuật", "Xuống dốc dài cần dùng phanh động cơ để tránh phanh quá nhiệt.", "Chọn số thấp trước dốc, giữ tốc độ ổn định và phanh theo nhịp khi cần.", "Về số N hoặc rà phanh liên tục có thể làm mất kiểm soát.", "Kiểm tra phanh trước đèo và dừng nếu có mùi khét bất thường."),
                    new("Lái xe ban đêm trên đường lạ", "Ban đêm khó ước lượng khoảng cách và dễ gặp người hoặc vật không phản quang.", "Giảm tốc, dùng đèn đúng chế độ và vệ sinh kính trước khi đi.", "Lạm dụng đèn pha gây chói xe đối diện; chạy nhanh hơn vùng chiếu sáng khiến không kịp xử lý.", "Chọn đường chính, nghỉ thường xuyên và tránh giờ cơ thể buồn ngủ nhất."),
                    new("Chia ca lái xe cho hành trình dài", "Đổi lái hiệu quả khi cả hai người đều đủ giấy phép và quen xe.", "Thống nhất điểm đổi lái, bàn giao tình trạng xe và điều chỉnh ghế gương trước khi đi.", "Đổi lái ở lề đường hoặc giao xe cho người đang mệt không giải quyết được rủi ro.", "Mỗi ca 1,5–2,5 giờ tùy điều kiện và sức khỏe."),
                    new("Xử lý khi xe thủng lốp trên cao tốc", "Mục tiêu đầu tiên là giữ hướng và đưa xe vào vị trí an toàn.", "Giảm ga từ từ, bật đèn cảnh báo, vào làn dừng khẩn cấp và đặt cảnh báo đúng khoảng cách.", "Phanh gấp hoặc thay lốp sát dòng xe đang chạy có thể gây tai nạn.", "Gọi cứu hộ nếu vị trí không an toàn hoặc không có dụng cụ phù hợp."),
                    new("Lái xe qua vùng ngập nước", "Không thể đánh giá chính xác độ sâu và tình trạng mặt đường dưới nước.", "Quan sát xe tương đương, đi chậm đều và tránh tạo sóng lớn.", "Cố đi qua nước cao quá nửa bánh hoặc khởi động lại khi xe chết máy dễ gây thủy kích.", "Quay đầu nếu không chắc chắn; an toàn quan trọng hơn tiết kiệm thời gian."),
                    new("Chuẩn bị bộ dụng cụ cho chuyến đi xa", "Một bộ dụng cụ gọn giúp xử lý sự cố nhỏ và chờ cứu hộ an toàn.", "Mang tam giác cảnh báo, bơm lốp, đèn pin, găng tay, dây câu bình và bộ sơ cứu.", "Dụng cụ không biết dùng hoặc hết pin sẽ không có giá trị khi cần.", "Kiểm tra định kỳ và đặt ở vị trí dễ lấy, không chôn dưới hành lý."),
                }),
            new(
                new[] { "xe điện", "pin", "trạm sạc" },
                new[] { "Hiểu đúng về quãng đường pin", "Lập kế hoạch sạc", "Kỹ thuật vận hành hiệu quả", "Xử lý tình huống bất thường" },
                new TopicSeed[]
                {
                    new("Lập kế hoạch sạc cho chuyến đi xe điện", "Quãng đường thực tế phụ thuộc tốc độ, điều hòa, tải trọng, địa hình và thời tiết.", "Xác định trạm sạc chính và một trạm dự phòng; đến trạm khi pin còn khoảng 15–25%.", "Để pin xuống quá thấp rồi mới tìm trạm làm giảm lựa chọn và tăng lo lắng.", "Kiểm tra ứng dụng trạm sạc, loại cổng, công suất và phương thức thanh toán."),
                    new("Phanh tái sinh hoạt động như thế nào", "Khi giảm ga, mô-tơ có thể thu hồi động năng để nạp lại pin.", "Làm quen mức tái sinh ở khu vực an toàn và quan sát giao thông để giảm ga sớm.", "Mức tái sinh mạnh có thể khiến người mới cảm thấy xe giảm tốc đột ngột.", "Không phụ thuộc hoàn toàn vào tái sinh; phanh cơ vẫn cần trong tình huống khẩn cấp."),
                    new("Điều hòa ảnh hưởng quãng đường xe điện", "Sưởi hoặc làm lạnh cabin sử dụng năng lượng từ pin kéo.", "Làm mát xe khi đang cắm sạc, chọn nhiệt độ hợp lý và dùng sưởi ghế nếu có.", "Đặt nhiệt độ cực thấp trong thời gian dài làm quãng đường dự báo giảm đáng kể.", "Theo dõi mức tiêu thụ phụ trợ trên màn hình xe."),
                    new("Sạc nhanh và sạc chậm khác nhau ra sao", "Sạc nhanh phù hợp hành trình; sạc chậm phù hợp qua đêm và thường nhẹ nhàng hơn với pin.", "Chọn công suất phù hợp xe, không phải trạm công suất cao nào cũng làm xe sạc nhanh hơn.", "Cố sạc nhanh đến 100% thường mất nhiều thời gian vì tốc độ giảm ở mức pin cao.", "Trong chuyến đi, sạc đến 70–85% có thể tối ưu thời gian."),
                    new("Thuê xe điện lần đầu cần biết gì", "Xe điện khởi động êm, phản hồi nhanh và có nhiều thông tin năng lượng trên màn hình.", "Nhờ hướng dẫn cách khởi động, chọn số, mở cổng sạc và dùng ứng dụng.", "Không nghe tiếng động cơ không có nghĩa xe chưa sẵn sàng di chuyển.", "Quan sát ký hiệu Ready và giữ chân phanh khi chọn số."),
                    new("Lái xe điện trên cao tốc", "Tốc độ cao làm tiêu thụ năng lượng tăng vì lực cản không khí.", "Giữ tốc độ ổn định, hạn chế tăng tốc mạnh và dự trù pin nhiều hơn so với đi phố.", "Dựa hoàn toàn vào quãng đường dự báo ban đầu mà không cập nhật theo điều kiện thực tế.", "Theo dõi mức pin đến trạm tiếp theo, không chỉ quãng đường còn lại."),
                    new("Xe điện đi đèo cần lưu ý gì", "Leo dốc tiêu hao nhiều pin nhưng xuống dốc có thể thu hồi một phần năng lượng.", "Bắt đầu chặng đèo với mức pin đủ, dùng tái sinh hợp lý và kiểm tra nhiệt độ hệ thống.", "Kỳ vọng thu hồi toàn bộ năng lượng đã dùng khi leo dốc là không thực tế.", "Luôn có dự phòng vì trạm sạc vùng núi có thể ít hoặc gián đoạn."),
                    new("Cách bàn giao xe điện khi kết thúc chuyến", "Bàn giao cần ghi nhận mức pin, cáp sạc và tình trạng cổng sạc.", "Sạc về mức đã thỏa thuận, tháo cáp đúng quy trình và chụp ảnh màn hình pin.", "Kéo cáp khi chưa mở khóa hoặc để cáp bẩn ướt có thể gây hư hỏng.", "Kiểm tra đủ phụ kiện trước khi rời xe."),
                    new("Ứng xử khi trạm sạc đang bận", "Trạm bận có thể làm thay đổi lịch trình, nhất là cuối tuần.", "Kiểm tra số trụ hoạt động, thời gian chờ và chuyển sang trạm dự phòng sớm.", "Chờ đến pin quá thấp mới quyết định đổi trạm làm tăng rủi ro.", "Duy trì mức dự phòng và tránh chiếm trụ sau khi sạc xong."),
                    new("Chăm sóc pin xe điện trong thời tiết nóng", "Nhiệt độ cao ảnh hưởng hiệu suất và hệ thống làm mát pin.", "Đỗ nơi râm, không để pin ở 100% quá lâu và ưu tiên sạc khi nhiệt độ đã ổn định.", "Sạc nhanh liên tục ngay sau chặng chạy nặng có thể làm tốc độ sạc giảm.", "Tuân theo cảnh báo của xe và không che các khe làm mát."),
                }),
            new(
                new[] { "bảo dưỡng", "dầu máy", "lốp" },
                new[] { "Dấu hiệu cần kiểm tra", "Quy trình kiểm tra cơ bản", "Khi nào phải dừng xe", "Lịch bảo dưỡng đề xuất" },
                new TopicSeed[]
                {
                    new("Nhận biết lốp ô tô cần thay", "Gai lốp mòn, nứt hông hoặc phồng lốp làm giảm độ bám và có nguy cơ nổ.", "Kiểm tra vạch báo mòn, tuổi lốp và độ mòn đều giữa các bánh.", "Chỉ nhìn áp suất mà bỏ qua vết nứt hoặc sửa lốp hông không đúng kỹ thuật.", "Đảo lốp theo lịch và thay theo tình trạng thực tế, không chỉ số kilomet."),
                    new("Kiểm tra dầu động cơ đúng cách", "Dầu bôi trơn bảo vệ động cơ và cần kiểm tra trên mặt phẳng khi máy đã ổn định.", "Rút que thăm, lau sạch, cắm lại rồi đọc mức giữa Min và Max.", "Châm quá nhiều dầu hoặc dùng sai cấp độ nhớt đều có thể gây hại.", "Theo lịch nhà sản xuất và điều kiện vận hành thực tế."),
                    new("Đèn cảnh báo trên bảng táp-lô nói gì", "Màu đỏ thường yêu cầu dừng kiểm tra; màu vàng cảnh báo cần xử lý sớm.", "Đọc hướng dẫn xe và ghi lại biểu tượng, thời điểm xuất hiện, triệu chứng đi kèm.", "Tiếp tục chạy khi đèn áp suất dầu hoặc nhiệt độ đỏ có thể gây hỏng nặng.", "Liên hệ hỗ trợ thay vì tự xóa lỗi khi chưa biết nguyên nhân."),
                    new("Bảo dưỡng hệ thống phanh", "Má phanh, đĩa, dầu phanh và lốp cùng quyết định quãng đường dừng.", "Chú ý tiếng rít, rung bàn đạp, xe lệch khi phanh hoặc hành trình bàn đạp dài.", "Trì hoãn kiểm tra vì xe vẫn phanh được có thể làm chi phí sửa tăng.", "Kiểm tra ngay khi có dấu hiệu và theo lịch định kỳ."),
                    new("Vệ sinh lọc gió điều hòa", "Lọc bẩn làm gió yếu, có mùi và giảm hiệu quả làm mát.", "Tháo theo hướng dẫn, thay đúng chiều và vệ sinh khu vực hộp lọc.", "Dùng khí nén quá mạnh hoặc lắp ngược làm lọc kém hiệu quả.", "Thay sớm hơn nếu thường đi đường bụi hoặc đô thị ô nhiễm."),
                    new("Kiểm tra nước làm mát an toàn", "Nước làm mát giữ nhiệt độ động cơ ổn định và hệ thống có áp suất khi nóng.", "Chỉ kiểm tra khi động cơ nguội, quan sát bình phụ và dấu rò rỉ.", "Mở nắp két nước khi nóng có thể gây bỏng nghiêm trọng.", "Dùng đúng loại dung dịch và xử lý rò rỉ sớm."),
                    new("Ắc quy yếu có dấu hiệu gì", "Khởi động chậm, đèn yếu hoặc cảnh báo điện là dấu hiệu thường gặp.", "Đo điện áp, kiểm tra cọc bình và tuổi ắc quy.", "Câu bình sai cực có thể làm hỏng hệ thống điện.", "Thay trước chuyến đi xa nếu bình đã yếu hoặc quá tuổi khuyến nghị."),
                    new("Cần gạt mưa và kính lái", "Tầm nhìn kém trong mưa thường do lưỡi gạt chai, kính bẩn hoặc thiếu nước rửa.", "Vệ sinh kính, kiểm tra cao su gạt và dùng dung dịch phù hợp.", "Gạt khô trên kính nhiều bụi làm xước bề mặt.", "Thay khi gạt để lại vệt hoặc phát tiếng bất thường."),
                    new("Điều hòa ô tô làm lạnh yếu", "Nguyên nhân có thể từ lọc gió, gas lạnh, quạt hoặc máy nén.", "Kiểm tra luồng gió, tiếng máy nén và nhiệt độ cửa gió.", "Tự nạp gas không đo áp suất có thể làm hệ thống hỏng.", "Đưa xe kiểm tra chuyên môn nếu làm lạnh giảm rõ rệt."),
                    new("Bảo dưỡng trước mùa mưa", "Mùa mưa yêu cầu lốp, phanh, đèn, gạt mưa và gioăng cửa hoạt động tốt.", "Kiểm tra thoát nước, độ sâu gai lốp và khả năng sấy kính.", "Bỏ qua nước đọng trong đèn hoặc thảm ẩm có thể dẫn đến chập điện và mùi mốc.", "Hoàn thành kiểm tra trước đợt mưa lớn, không chờ xe có sự cố."),
                }),
            new(
                new[] { "kiểm tra xe", "an toàn", "trước chuyến" },
                new[] { "Quan sát ngoại thất", "Kiểm tra trong khoang lái", "Chạy thử và ghi nhận", "Tiêu chí từ chối nhận xe" },
                new TopicSeed[]
                {
                    new("Checklist 10 phút trước khi nhận xe", "Một vòng kiểm tra ngắn giúp phát hiện vết xước, lốp non và trang bị thiếu.", "Đi quanh xe theo một chiều, quay video và đối chiếu biên bản bàn giao.", "Kiểm tra rời rạc dễ bỏ sót cùng một khu vực.", "Thân vỏ, kính, lốp, đèn, nhiên liệu, giấy tờ, điều hòa và dụng cụ."),
                    new("Kiểm tra phanh trước khi rời bãi", "Phanh cần phản hồi đều, không rung mạnh và không có đèn cảnh báo.", "Thử ở tốc độ thấp trong khu vực an toàn trước khi nhập đường chính.", "Thử phanh gấp trong bãi đông hoặc bỏ qua cảm giác bàn đạp mềm.", "Yêu cầu đổi xe nếu phanh bất thường hoặc có tiếng kim loại."),
                    new("Chỉnh ghế và gương đúng tư thế", "Tư thế đúng giúp kiểm soát vô-lăng, đạp phanh hết hành trình và giảm điểm mù.", "Chỉnh ghế trước, sau đó vô-lăng, gương trong và gương ngoài.", "Chỉnh gương khi xe đang chạy hoặc ngồi quá xa vô-lăng làm phản ứng chậm.", "Cổ tay chạm đỉnh vô-lăng khi vai vẫn tựa ghế là mốc tham khảo."),
                    new("Kiểm tra lốp trước chuyến đi", "Lốp quyết định độ bám, phanh và khả năng chịu tải.", "Quan sát áp suất, vết cắt, đinh và độ mòn từng bánh.", "Chỉ kiểm tra bánh nhìn thấy bên ngoài mà bỏ qua mặt trong.", "Không nhận xe nếu lốp phồng, nứt sâu hoặc mòn đến vạch."),
                    new("Kiểm tra giấy tờ và bảo hiểm xe thuê", "Giấy đăng ký, đăng kiểm và bảo hiểm phải còn hiệu lực và phù hợp biển số.", "Chụp ảnh hoặc lưu bản điện tử được đơn vị cho phép.", "Không biết số hỗ trợ bảo hiểm sẽ làm xử lý sự cố chậm.", "Xác nhận phạm vi bảo hiểm và mức miễn thường trước khi đi."),
                    new("Kiểm tra camera và cảm biến lùi", "Hệ thống hỗ trợ giúp đỗ xe nhưng không thay thế quan sát trực tiếp.", "Lau camera, thử hình ảnh và âm thanh cảm biến trong bãi.", "Tin hoàn toàn vào màn hình có thể bỏ sót vật thấp hoặc điểm mù bên hông.", "Luôn quay đầu quan sát và di chuyển chậm khi lùi."),
                    new("Kiểm tra điều hòa trước chuyến đi", "Điều hòa ảnh hưởng sức khỏe và khả năng tập trung, nhất là trời nóng.", "Thử các mức quạt, hướng gió, sấy kính và điều hòa hàng sau.", "Chỉ thử trong vài giây có thể không phát hiện hệ thống làm lạnh yếu.", "Cho xe hoạt động vài phút và kiểm tra mùi bất thường."),
                    new("Kiểm tra dụng cụ cứu hộ trên xe", "Xe nên có lốp dự phòng hoặc bộ vá, kích, cờ lê và tam giác cảnh báo.", "Kiểm tra vị trí, tình trạng và cách sử dụng trước khi đi.", "Có dụng cụ nhưng thiếu chìa khóa mở ốc hoặc lốp dự phòng hết hơi vẫn vô ích.", "Yêu cầu bổ sung nếu thiếu trang bị được cam kết."),
                    new("Ghi nhận vết xước khi nhận xe", "Ảnh và video có thời gian giúp bảo vệ cả khách hàng và đơn vị cho thuê.", "Chụp toàn cảnh rồi cận cảnh từng vết, gồm mâm và gầm cản.", "Ảnh quá tối hoặc không thấy biển số khó dùng để đối chiếu.", "Yêu cầu ghi vết xước vào biên bản, không chỉ nhắn miệng."),
                    new("Khi nào nên từ chối nhận xe", "Khách hàng có quyền từ chối nếu xe không an toàn hoặc khác đáng kể so với thỏa thuận.", "Nêu rõ lỗi, chụp bằng chứng và yêu cầu đổi xe hoặc hoàn cọc theo chính sách.", "Cố sử dụng xe có đèn cảnh báo đỏ, lốp hỏng hoặc giấy tờ hết hạn là không nên.", "Ưu tiên an toàn hơn lịch trình; không ký xác nhận xe đạt nếu còn nghi ngờ."),
                }),
        };

        public static BlogArticleContent Build(int blogId, string? originalTitle)
        {
            var normalizedTitle = (originalTitle ?? string.Empty).ToLowerInvariant();
            var familyIndex = blogId is >= 1 and <= 160
                ? (blogId - 1) % Families.Length
                : Array.FindIndex(Families, family => family.Keywords.Any(normalizedTitle.Contains));
            if (familyIndex < 0)
            {
                familyIndex = Math.Abs((blogId - 1) % Families.Length);
            }

            var family = Families[familyIndex];
            var sequenceInFamily = Math.Max(0, (blogId - 1) / Families.Length);
            var seed = family.Seeds[sequenceInFamily % family.Seeds.Length];
            var advancedVariant = sequenceInFamily >= family.Seeds.Length;

            var audience = advancedVariant
                ? "Bài viết đi sâu vào các tình huống thực tế dành cho người đã có kinh nghiệm và muốn chuẩn bị hành trình kỹ hơn."
                : "Bài viết trình bày theo từng bước, phù hợp với người lần đầu tự chuẩn bị hoặc thuê xe cho hành trình.";
            var displayTitle = seed.Title + (advancedVariant ? " – kinh nghiệm nâng cao" : " – hướng dẫn thực tế");

            return new BlogArticleContent
            {
                DisplayTitle = displayTitle,
                Introduction = $"{audience} {seed.Focus}",
                Sections = new List<BlogArticleSection>
                {
                    new() { Heading = family.Headings[0], Content = seed.Focus + (advancedVariant ? " Với hành trình dài hoặc lịch trình nhiều điểm, cần ghi rõ các giả định về thời gian, tải trọng và điều kiện đường để tránh quyết định dựa trên cảm tính." : " Người đọc nên bắt đầu từ nhu cầu thực tế thay vì chọn theo thói quen hoặc hình thức.") },
                    new() { Heading = family.Headings[1], Content = seed.Practice + (advancedVariant ? " Nên lập phương án chính và phương án dự phòng, đồng thời xác định mốc kiểm tra giữa hành trình để điều chỉnh kịp thời." : " Thực hiện lần lượt từng bước sẽ giúp dễ kiểm soát và giảm việc phải xử lý gấp.") },
                    new() { Heading = family.Headings[2], Content = seed.Risk + (advancedVariant ? " Khi phát hiện dấu hiệu bất thường, ưu tiên dừng ở vị trí an toàn và liên hệ hỗ trợ thay vì cố tiếp tục để giữ lịch." : " Đây là nhóm sai lầm phổ biến nhưng có thể phòng tránh nếu kiểm tra trước.") },
                    new() { Heading = family.Headings[3], Content = seed.Checklist + (advancedVariant ? " Sau chuyến đi, nên ghi lại mức tiêu hao, thời gian thực tế và vấn đề phát sinh để lần sau lập kế hoạch chính xác hơn." : " Hãy kiểm tra từng mục trước khi khởi hành và xác nhận lại với người đồng hành.") },
                }
            };
        }
    }
}